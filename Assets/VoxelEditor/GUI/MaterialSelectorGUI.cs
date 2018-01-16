﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MaterialSelectorGUI : GUIPanel
{
    private static Texture2D whiteTexture;
    private const int NUM_COLUMNS = 4;
    private const int TEXTURE_MARGIN = 10;
    private const float CATEGORY_BUTTON_ASPECT = 3.0f;
    private const string BACK_BUTTON = "Back";

    public delegate void MaterialSelectHandler(Material material);

    public MaterialSelectHandler handler;
    public string rootDirectory = "GameAssets/Materials";
    public bool allowNullMaterial = false;
    public bool closeOnSelect = true;
    public Material highlightMaterial = null;

    private int tab;
    private string materialDirectory;
    private List<Material> materials;
    private List<string> materialSubDirectories;

    private GUIStyle condensedButtonStyle = null;

    public void Start()
    {
        materialDirectory = rootDirectory;
        UpdateMaterialDirectory();
        tab = 1; // TODO: choose based on material type
    }

    public override Rect GetRect(float width, float height)
    {
        return new Rect(width * .25f, height * .1f, width * .5f, height * .8f);
    }

    public override void WindowGUI()
    {
        if (condensedButtonStyle == null)
        {
            condensedButtonStyle = new GUIStyle(GUI.skin.button);
            condensedButtonStyle.padding.left = 16;
            condensedButtonStyle.padding.right = 16;
        }

        if (allowNullMaterial)
            tab = GUILayout.SelectionGrid(tab, new string[] { "Color", "Texture", "None" }, 3);
        else
            tab = GUILayout.SelectionGrid(tab, new string[] { "Color", "Texture" }, 2);

        if (tab == 0)
            ColorTab();
        if (tab == 1)
            TextureTab();
        if (tab == 2)
            NoneTab();
    }

    private void ColorTab()
    {

    }

    private void TextureTab()
    {
        if (materials == null)
            return;
        scroll = GUILayout.BeginScrollView(scroll);
        Rect rowRect = new Rect();
        for (int i = 0; i < materialSubDirectories.Count; i++)
        {
            if (i % NUM_COLUMNS == 0)
                rowRect = GUILayoutUtility.GetAspectRect(NUM_COLUMNS * CATEGORY_BUTTON_ASPECT);
            Rect buttonRect = rowRect;
            buttonRect.width = buttonRect.height * CATEGORY_BUTTON_ASPECT;
            buttonRect.x = buttonRect.width * (i % NUM_COLUMNS);
            string subDir = materialSubDirectories[i];
            bool selected;
            if (subDir == BACK_BUTTON)
                // highlight the button
                selected = !GUI.Toggle(buttonRect, true, subDir, condensedButtonStyle);
            else
                selected = GUI.Button(buttonRect, subDir, condensedButtonStyle);
            if (selected)
            {
                scroll = new Vector2(0, 0);
                MaterialDirectorySelected(materialSubDirectories[i]);
            }
        }
        for (int i = 0; i < materials.Count; i++)
        {
            if (i % NUM_COLUMNS == 0)
                rowRect = GUILayoutUtility.GetAspectRect(NUM_COLUMNS);
            Rect buttonRect = rowRect;
            buttonRect.width = buttonRect.height;
            buttonRect.x = buttonRect.width * (i % NUM_COLUMNS);
            Rect textureRect = new Rect(
                buttonRect.xMin + TEXTURE_MARGIN, buttonRect.yMin + TEXTURE_MARGIN,
                buttonRect.width - TEXTURE_MARGIN * 2, buttonRect.height - TEXTURE_MARGIN * 2);
            Material material = materials[i];
            bool selected;
            if (material == highlightMaterial)
                // highlight the button
                selected = !GUI.Toggle(buttonRect, true, "", GUI.skin.button);
            else
                selected = GUI.Button(buttonRect, "");
            if (selected)
                MaterialSelected(material);
            DrawMaterialTexture(material, textureRect, false);
        }
        GUILayout.EndScrollView();
    }

    private void NoneTab()
    {
        if (highlightMaterial != null)
            MaterialSelected(null);
    }

    void UpdateMaterialDirectory()
    {
        materialSubDirectories = new List<string>();
        if (materialDirectory != rootDirectory)
            materialSubDirectories.Add(BACK_BUTTON);
        materials = new List<Material>();
        foreach (string dirEntry in ResourcesDirectory.dirList)
        {
            if (dirEntry.Length <= 2)
                continue;
            string newDirEntry = dirEntry.Substring(2);
            if (Path.GetFileName(newDirEntry).StartsWith("$"))
                continue; // special alternate materials for game
            string directory = Path.GetDirectoryName(newDirEntry);
            if (directory != materialDirectory)
                continue;
            string extension = Path.GetExtension(newDirEntry);
            if (extension == "")
                materialSubDirectories.Add(Path.GetFileName(newDirEntry));
            else if (extension == ".mat")
                materials.Add(ResourcesDirectory.GetMaterial(newDirEntry));
        }

        Resources.UnloadUnusedAssets();
    }

    private void MaterialDirectorySelected(string name)
    {
        if (name == BACK_BUTTON)
        {
            if (materialDirectory.Trim() != "")
                materialDirectory = Path.GetDirectoryName(materialDirectory);
            UpdateMaterialDirectory();
            return;
        }
        else
        {
            if (materialDirectory.Trim() == "")
                materialDirectory = name;
            else
                materialDirectory += "/" + name;
            UpdateMaterialDirectory();
        }
    }

    private void MaterialSelected(Material material)
    {
        highlightMaterial = material;
        if (handler != null)
            handler(material);
        if (closeOnSelect)
            Destroy(this);
    }

    public static void DrawMaterialTexture(Material mat, Rect rect, bool alpha)
    {
        if (mat == null)
            return;
        if (whiteTexture == null)
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }
        Rect texCoords = new Rect(Vector2.zero, Vector2.one);
        Texture texture = whiteTexture;
        if (mat.mainTexture != null)
        {
            texture = mat.mainTexture;
            texCoords = new Rect(Vector2.zero, mat.mainTextureScale);
        }
        else if (mat.HasProperty("_ColorControl"))
            // water shader
            texture = mat.GetTexture("_ColorControl");
        else if (mat.HasProperty("_FrontTex"))
            // skybox
            texture = mat.GetTexture("_FrontTex");

        Color baseColor = GUI.color;
        if (mat.HasProperty("_Color"))
            GUI.color *= mat.color;
        GUI.DrawTextureWithTexCoords(rect, texture, texCoords, alpha);
        GUI.color = baseColor;
    }
}
