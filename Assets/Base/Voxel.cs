﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct VoxelFace
{
    public Material material;
    public Material overlay;
    public byte orientation;
    public bool addSelected, storedSelected;
    public bool selected
    {
        get
        {
            return (!IsEmpty()) && (addSelected || storedSelected);
        }
    }

    public bool IsEmpty()
    {
        return material == null && overlay == null;
    }

    public void Clear()
    {
        material = null;
        overlay = null;
        orientation = 0;
        addSelected = false;
        storedSelected = false;
    }

    public static int GetOrientationRotation(byte orientation)
    {
        return orientation & 3;
    }

    public static bool GetOrientationMirror(byte orientation)
    {
        return (orientation & 4) != 0;
    }

    public static byte Orientation(int rotation, bool mirror)
    {
        while (rotation < 0)
            rotation += 4;
        rotation %= 4;
        return (byte)(rotation + (mirror ? 4 : 0));
    }
}

public struct VoxelFaceReference
{
    public Voxel voxel;
    public int faceI;

    public VoxelFaceReference(Voxel voxel, int faceI)
    {
        this.voxel = voxel;
        this.faceI = faceI;
    }

    public VoxelFace face
    {
        get
        {
            return voxel.faces[faceI];
        }
    }
}

public class Voxel : MonoBehaviour
{

    public static VoxelFace EMPTY_FACE = new VoxelFace();
    public static Vector2[] SQUARE_LOOP = new Vector2[]
    {
        Vector2.zero,
        Vector2.right,
        Vector2.one,
        Vector2.up
    };

    public static Material selectedMaterial; // set by VoxelArray instance
    public static Material xRayMaterial;

    public static Vector3 DirectionForFaceI(int faceI)
    {
        switch (faceI)
        {
            case 0:
                return Vector3.left;
            case 1:
                return Vector3.right;
            case 2:
                return Vector3.down;
            case 3:
                return Vector3.up;
            case 4:
                return Vector3.back;
            case 5:
                return Vector3.forward;
            default:
                return Vector3.zero;
        }
    }

    public static Vector3 OppositeDirectionForFaceI(int faceI)
    {
        return DirectionForFaceI(OppositeFaceI(faceI));
    }

    public static int FaceIForDirection(Vector3 direction)
    {
        if (direction == Vector3.left)
            return 0;
        else if (direction == Vector3.right)
            return 1;
        else if (direction == Vector3.down)
            return 2;
        else if (direction == Vector3.up)
            return 3;
        else if (direction == Vector3.back)
            return 4;
        else if (direction == Vector3.forward)
            return 5;
        else
            return -1;
    }

    public static int OppositeFaceI(int faceI)
    {
        return (faceI / 2) * 2 + (faceI % 2 == 0 ? 1 : 0);
    }

    public static int SideFaceI(int faceI, int sideNum)
    {
        sideNum %= 4;
        faceI = (faceI / 2) * 2 + 2 + sideNum;
        faceI %= 6;
        return faceI;
    }

    public static int FaceIAxis(int faceI)
    {
        return faceI / 2;
    }

    private static bool InEditor()
    {
        // TODO: better way to check this?
        return SceneManager.GetActiveScene().name == "editScene";
    }

    public VoxelFace[] faces = new VoxelFace[6]; // xMin, xMax, yMin, yMax, zMin, zMax
    public int substanceDebug = 0;
    private Substance _substance = null;
    public Substance substance
    {
        get
        {
            return _substance;
        }
        set
        {
            if (_substance != null)
                _substance.RemoveVoxel(this);
            _substance = value;
            if (_substance == null)
                substanceDebug = 0;
            else
            {
                substanceDebug = _substance.GetHashCode();
                _substance.AddVoxel(this);
            }
        }
    }

	void Start ()
    {
        UpdateVoxel();
	}

    public Bounds GetFaceBounds(int faceI)
    {
        Bounds bounds;
        switch (faceI)
        {
            case 0:
                bounds = new Bounds(new Vector3(0, 0.5f, 0.5f), new Vector3(0, 1, 1));
                break;
            case 1:
                bounds = new Bounds(new Vector3(1, 0.5f, 0.5f), new Vector3(0, 1, 1));
                break;
            case 2:
                bounds = new Bounds(new Vector3(0.5f, 0, 0.5f), new Vector3(1, 0, 1));
                break;
            case 3:
                bounds = new Bounds(new Vector3(0.5f, 1, 0.5f), new Vector3(1, 0, 1));
                break;
            case 4:
                bounds = new Bounds(new Vector3(0.5f, 0.5f, 0), new Vector3(1, 1, 0));
                break;
            case 5:
                bounds = new Bounds(new Vector3(0.5f, 0.5f, 1), new Vector3(1, 1, 0));
                break;
            default:
                bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0, 0, 0));
                break;
        }
        bounds.center += transform.position;
        return bounds;
    }

    public Bounds GetBounds()
    {
        return new Bounds(transform.position + new Vector3(0.5f,0.5f,0.5f), Vector3.one);
    }

    public bool IsEmpty()
    {
        if (substance != null)
            return false;
        foreach (VoxelFace face in faces)
        {
            if (!face.IsEmpty())
                return false;
        }
        return true;
    }

    public void Clear()
    {
        for (int faceI = 0; faceI < faces.Length; faceI++)
        {
            faces[faceI].Clear();
        }
        substance = null;
    }

    void OnDestroy()
    {
        substance = null; // remove from substance
        VoxelArray array = transform.parent.GetComponent<VoxelArray>();
        array.VoxelDestroyed(this);
    }

    public void UpdateVoxel()
    {
        bool inEditor = InEditor();

        int numFilledFaces = 0;
        foreach (VoxelFace f in faces)
            if (!f.IsEmpty())
                numFilledFaces++;

        var vertices = new Vector3[numFilledFaces * 4];
        var uv = new Vector2[numFilledFaces * 4];

        float[] transformPos = new float[]
        {
            transform.position.x, transform.position.y, transform.position.z
        };
        numFilledFaces = 0;
        int numMaterials = 0;
        for (int faceNum = 0; faceNum < 6; faceNum++)
        {
            VoxelFace face = faces[faceNum];
            if (face.IsEmpty())
                continue;
            int axis = FaceIAxis(faceNum);

            // example for faceNum = 5 (z min)
            // 0 bottom left
            // 1 bottom right
            // 2 top right
            // 3 top left
            for (int i = 0; i < 4; i++)
            {
                int vertexI = numFilledFaces * 4 + i;
                float[] vertexPos = new float[3];
                vertexPos[axis] = faceNum % 2;
                vertexPos[(axis + 1) % 3] = SQUARE_LOOP[i].x;
                vertexPos[(axis + 2) % 3] = SQUARE_LOOP[i].y;
                vertices[vertexI] = new Vector3(vertexPos[0], vertexPos[1], vertexPos[2]);
                int uvNum = VoxelFace.GetOrientationRotation(face.orientation);
                if (faceNum == 1 || faceNum == 2 || faceNum == 4)
                    uvNum += 1;
                if ((VoxelFace.GetOrientationMirror(face.orientation) ^ (faceNum % 2) == 1))
                    uvNum += i;
                else
                    uvNum += 4 - i;
                uvNum %= 4;
                Vector2 uvOrigin; // materials can span multiple voxels
                if (VoxelFace.GetOrientationRotation(face.orientation) % 2 == 1 ^ (faceNum == 0 || faceNum == 1))
                    uvOrigin = new Vector2(transformPos[(axis + 2) % 3], transformPos[(axis + 1) % 3]);
                else
                    uvOrigin = new Vector2(transformPos[(axis + 1) % 3], transformPos[(axis + 2) % 3]);
                uv[vertexI] =  uvOrigin + SQUARE_LOOP[uvNum];
            }

            numFilledFaces++;
            if (face.material != null)
                numMaterials++;
            if (face.overlay != null)
                numMaterials++;
            if (face.selected)
                numMaterials++;
        }
        
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.name = "Voxel Mesh";
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.subMeshCount = numMaterials;

        Material[] materials = new Material[numMaterials];

        numFilledFaces = 0;
        numMaterials = 0;
        for (int faceI = 0; faceI < 6; faceI++)
        {
            VoxelFace face = faces[faceI];
            if (face.IsEmpty())
                continue;

            var triangles = new int[6];

            if (faceI % 2 == 1)
            {
                triangles[0] = numFilledFaces * 4 + 0;
                triangles[1] = numFilledFaces * 4 + 1;
                triangles[2] = numFilledFaces * 4 + 2;
                triangles[3] = numFilledFaces * 4 + 0;
                triangles[4] = numFilledFaces * 4 + 2;
                triangles[5] = numFilledFaces * 4 + 3;
            }
            else
            {
                triangles[0] = numFilledFaces * 4 + 0;
                triangles[1] = numFilledFaces * 4 + 2;
                triangles[2] = numFilledFaces * 4 + 1;
                triangles[3] = numFilledFaces * 4 + 0;
                triangles[4] = numFilledFaces * 4 + 3;
                triangles[5] = numFilledFaces * 4 + 2;
            }

            if (face.material != null) {
                materials[numMaterials] = face.material;
                mesh.SetTriangles(triangles, numMaterials);
                numMaterials++;
            }
            if (face.overlay != null)
            {
                materials[numMaterials] = face.overlay;
                mesh.SetTriangles(triangles, numMaterials);
                numMaterials++;
            }
            if (face.selected)
            {
                materials[numMaterials] = selectedMaterial;
                mesh.SetTriangles(triangles, numMaterials);
                numMaterials++;
            }

            numFilledFaces++;
        }
        
        mesh.RecalculateNormals();

        bool xRay = false;
        if (substance != null && inEditor)
            xRay = substance.xRay;

        Renderer renderer = GetComponent<Renderer>();
        if (xRay)
        {
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] != selectedMaterial)
                    materials[i] = xRayMaterial;
        }
        renderer.materials = materials;
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        BoxCollider boxCollider = GetComponent<BoxCollider>();

        if (inEditor)
        {
            renderer.enabled = true;
            meshCollider.enabled = true;
            meshCollider.sharedMesh = mesh;
            boxCollider.enabled = false;
        }
        else
        {
            boxCollider.enabled = true;
            if (substance == null)
            {
                renderer.enabled = true;
                boxCollider.isTrigger = false;
            }
            else
            {
                boxCollider.isTrigger = !substance.solid;
                renderer.enabled = substance.visible;
            }
            meshCollider.sharedMesh = null;
            meshCollider.enabled = false;
        }
    } // end UpdateVoxel()
}