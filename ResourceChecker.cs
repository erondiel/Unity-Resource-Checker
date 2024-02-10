// Resource Checker
// (c) 2012 Simon Oliver / HandCircus / hello@handcircus.com
// Public domain, do with whatever you like, commercial or not
// This comes with no warranty, use at your own risk!
// https://github.com/handcircus/Unity-Resource-Checker
// Updated by Rodrigo Camacho to read ASTC compression
// https://github.com/erondiel/Unity-Resource-Checker




using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.U2D;
using UnityEngine.UI;

public class TextureDetails
{
    public bool isCubeMap;
    public int memSizeKB;
    public Texture texture;
    public TextureFormat format;
    public int mipMapCount;
    public List<UnityEngine.Object> FoundInMaterials=new List<UnityEngine.Object>();
    public List<UnityEngine.Object> FoundInRenderers = new List<UnityEngine.Object>();
    public TextureDetails()
    {

    }
};

public class ImageDetails
{
	public Image image;
	public bool packed;
	public TextureFormat format;
	public List<UnityEngine.Object> FoundInObjects=new List<UnityEngine.Object>();
}

public class MaterialDetails
{

    public Material material;

    public List<Renderer> FoundInRenderers=new List<Renderer>();

    public MaterialDetails()
    {

    }
};

public class MeshDetails
{

    public Mesh mesh;

    public List<MeshFilter> FoundInMeshFilters=new List<MeshFilter>();
    public List<SkinnedMeshRenderer> FoundInSkinnedMeshRenderer=new List<SkinnedMeshRenderer>();

    public MeshDetails()
    {

    }
};

public class ResourceChecker : EditorWindow {


    string[] inspectToolbarStrings = {"Textures", "Materials","Meshes","Images"};

    enum InspectType
    {
        Textures,Materials,Meshes,Images
    };

    InspectType ActiveInspectType=InspectType.Textures;

    float ThumbnailWidth=40;
    float ThumbnailHeight=40;

    List<TextureDetails> ActiveTextures=new List<TextureDetails>();
    List<MaterialDetails> ActiveMaterials=new List<MaterialDetails>();
    List<MeshDetails> ActiveMeshDetails=new List<MeshDetails>();
	List<ImageDetails> ActiveImages = new List<ImageDetails>();

    Vector2 textureListScrollPos=new Vector2(0,0);
    Vector2 materialListScrollPos=new Vector2(0,0);
    Vector2 meshListScrollPos=new Vector2(0,0);
	Vector2 imageListScrollPos=new Vector2(0,0);

    int TotalTextureMemory=0;
    int TotalMeshVertices=0;

    bool ctrlPressed=false;
    List<SpriteAtlas> _spriteAtlases;


    static int MinWidth=455;


    [MenuItem("Window/Level Design/Resource Checker")]
    static void Init ()
    {
        ResourceChecker window = (ResourceChecker) EditorWindow.GetWindow (typeof (ResourceChecker));
        window.CheckResources();
        window.minSize=new Vector2(MinWidth,300);
    }

    void OnGUI ()
    {
        if (GUILayout.Button("Refresh")) CheckResources();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Materials "+ActiveMaterials.Count);
        GUILayout.Label("Textures "+ActiveTextures.Count+" - "+FormatSizeString(TotalTextureMemory));
        GUILayout.Label("Meshes "+ActiveMeshDetails.Count+" - "+TotalMeshVertices+" verts");
		GUILayout.Label("Images "+ActiveImages.Count);
        GUILayout.EndHorizontal();
        ActiveInspectType=(InspectType)GUILayout.Toolbar((int)ActiveInspectType,inspectToolbarStrings);

        ctrlPressed=Event.current.control || Event.current.command;

        switch (ActiveInspectType)
        {
            case InspectType.Textures:
                ListTextures();
                break;
            case InspectType.Materials:
                ListMaterials();
                break;
            case InspectType.Meshes:
                ListMeshes();
                break;
			case InspectType.Images:
				ListImages();
				break;
        }
    }


    int GetBitsPerPixel(TextureFormat format)
    {
        // Handling ASTC formats with if-else conditions
        if (format == TextureFormat.ASTC_4x4 || format == TextureFormat.ASTC_4x4) return 8;
        else if (format == TextureFormat.ASTC_5x5 || format == TextureFormat.ASTC_5x5) return (int)Math.Round(5.12f);
        else if (format == TextureFormat.ASTC_6x6 || format == TextureFormat.ASTC_6x6) return (int)Math.Round(3.56f);
        else if (format == TextureFormat.ASTC_8x8 || format == TextureFormat.ASTC_8x8) return 2;
        else if (format == TextureFormat.ASTC_10x10 || format == TextureFormat.ASTC_10x10) return (int)Math.Round(1.28f);
        else if (format == TextureFormat.ASTC_12x12 || format == TextureFormat.ASTC_12x12) return (int)Math.Round(0.89f);
        else
        {
            switch (format)
            {

                case TextureFormat.Alpha8: //    Alpha-only texture format.
                    return 8;
                case TextureFormat.ARGB4444: //  A 16 bits/pixel texture format. Texture stores color with an alpha channel.
                    return 16;
                case TextureFormat.RGBA4444: //  A 16 bits/pixel texture format.
                    return 16;
                case TextureFormat.RGB24:   // A color texture format.
                    return 24;
                case TextureFormat.RGBA32:  //Color with an alpha channel texture format.
                    return 32;
                case TextureFormat.ARGB32:  //Color with an alpha channel texture format.
                    return 32;
                case TextureFormat.RGB565:  //   A 16 bit color texture format.
                    return 16;
                case TextureFormat.DXT1:    // Compressed color texture format.
                    return 4;
                case TextureFormat.DXT5:    // Compressed color with alpha channel texture format.
                    return 8;
                /*
            case TextureFormat.WiiI4:   // Wii texture format.
            case TextureFormat.WiiI8:   // Wii texture format. Intensity 8 bit.
            case TextureFormat.WiiIA4:  // Wii texture format. Intensity + Alpha 8 bit (4 + 4).
            case TextureFormat.WiiIA8:  // Wii texture format. Intensity + Alpha 16 bit (8 + 8).
            case TextureFormat.WiiRGB565:   // Wii texture format. RGB 16 bit (565).
            case TextureFormat.WiiRGB5A3:   // Wii texture format. RGBA 16 bit (4443).
            case TextureFormat.WiiRGBA8:    // Wii texture format. RGBA 32 bit (8888).
            case TextureFormat.WiiCMPR: //   Compressed Wii texture format. 4 bits/texel, ~RGB8A1 (Outline alpha is not currently supported).
                return 0;  //Not supported yet
            */
                case TextureFormat.PVRTC_RGB2://     PowerVR (iOS) 2 bits/pixel compressed color texture format.
                    return 2;
                case TextureFormat.PVRTC_RGBA2://    PowerVR (iOS) 2 bits/pixel compressed with alpha channel texture format
                    return 2;
                case TextureFormat.PVRTC_RGB4://     PowerVR (iOS) 4 bits/pixel compressed color texture format.
                    return 4;
                case TextureFormat.PVRTC_RGBA4://    PowerVR (iOS) 4 bits/pixel compressed with alpha channel texture format
                    return 4;
                case TextureFormat.ETC_RGB4://   ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
                    return 4;
                // case TextureFormat.ETC_RGB4://   ATC (ATITC) 4 bits/pixel compressed RGB texture format.
                // return 4;
                case TextureFormat.ETC2_RGBA8://  ATC (ATITC) 8 bits/pixel compressed RGB texture format.
                    return 8;
                case TextureFormat.BGRA32://     Format returned by iPhone camera
                    return 32;
                    //case TextureFormat.ATF_RGB_DXT1://   Flash-specific RGB DXT1 compressed color texture format.
                    //case TextureFormat.ATF_RGBA_JPG://   Flash-specific RGBA JPG-compressed color texture format.
                    //case TextureFormat.ATF_RGB_JPG://    Flash-specific RGB JPG-compressed color texture format.
                    //   return 0; //Not supported yet
            }
        }
        return 0;
    }

    int CalculateTextureSizeBytes(Texture tTexture)
    {

        int tWidth=tTexture.width;
        int tHeight=tTexture.height;
        if (tTexture is Texture2D)
        {
            Texture2D tTex2D=tTexture as Texture2D;
            int bitsPerPixel=GetBitsPerPixel(tTex2D.format);
            int mipMapCount=tTex2D.mipmapCount;
            int mipLevel=1;
            int tSize=0;
            while (mipLevel<=mipMapCount)
            {
                tSize+=tWidth*tHeight*bitsPerPixel/8;
                tWidth=tWidth/2;
                tHeight=tHeight/2;
                mipLevel++;
            }
            return tSize;
        }

        if (tTexture is Cubemap)
        {
            Cubemap tCubemap=tTexture as Cubemap;
            int bitsPerPixel=GetBitsPerPixel(tCubemap.format);
            return tWidth*tHeight*6*bitsPerPixel/8;
        }
        return 0;
    }


    void SelectObject(UnityEngine.Object selectedObject,bool append)
    {
        if (append)
        {
            List<UnityEngine.Object> currentSelection=new List<UnityEngine.Object>(Selection.objects);
            // Allow toggle selection
            if (currentSelection.Contains(selectedObject)) currentSelection.Remove(selectedObject);
            else currentSelection.Add(selectedObject);

            Selection.objects=currentSelection.ToArray();
        }
        else Selection.activeObject=selectedObject;
    }

    void SelectObjects(List<UnityEngine.Object> selectedObjects,bool append)
    {
        if (append)
        {
            List<UnityEngine.Object> currentSelection=new List<UnityEngine.Object>(Selection.objects);
            currentSelection.AddRange(selectedObjects);
            Selection.objects=currentSelection.ToArray();
        }
        else Selection.objects=selectedObjects.ToArray();
    }

	void ListImages()
	{
		imageListScrollPos = EditorGUILayout.BeginScrollView(imageListScrollPos);

		foreach (ImageDetails imgDetails in ActiveImages)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Box(imgDetails.image.mainTexture, GUILayout.Width(ThumbnailWidth), GUILayout.Height(ThumbnailHeight));

			if(GUILayout.Button(imgDetails.image.name,GUILayout.Width(150)))
			{
				SelectObject(imgDetails.image.mainTexture,ctrlPressed);
			}

			string sizeLabel=""+imgDetails.image.mainTexture.width+"x"+imgDetails.image.mainTexture.height + " - " + imgDetails.format;

			GUILayout.Label (sizeLabel,GUILayout.Width(120));

			GUILayout.Label ("packed: " + imgDetails.packed ,GUILayout.Width(120));

			if(GUILayout.Button(imgDetails.FoundInObjects.Count+" GO",GUILayout.Width(50)))
			{
				List<UnityEngine.Object> FoundObjects=new List<UnityEngine.Object>();
				foreach (Image imgs in imgDetails.FoundInObjects) FoundObjects.Add(imgs.gameObject);
				SelectObjects(FoundObjects,ctrlPressed);
			}

			GUILayout.EndHorizontal();
		}

		EditorGUILayout.EndScrollView();
	}

    void ListTextures()
    {
        textureListScrollPos = EditorGUILayout.BeginScrollView(textureListScrollPos);

        foreach (TextureDetails tDetails in ActiveTextures)
        {

            GUILayout.BeginHorizontal ();
            GUILayout.Box(tDetails.texture, GUILayout.Width(ThumbnailWidth), GUILayout.Height(ThumbnailHeight));

            if(GUILayout.Button(tDetails.texture.name,GUILayout.Width(150)))
            {
                SelectObject(tDetails.texture,ctrlPressed);
            }

            string sizeLabel=""+tDetails.texture.width+"x"+tDetails.texture.height;
            if (tDetails.isCubeMap) sizeLabel+="x6";
            sizeLabel+=" - "+tDetails.mipMapCount+"mip";
            sizeLabel+="\n"+FormatSizeString(tDetails.memSizeKB)+" - "+tDetails.format+"";

            GUILayout.Label (sizeLabel,GUILayout.Width(120));

            if(GUILayout.Button(tDetails.FoundInMaterials.Count+" Mat",GUILayout.Width(50)))
            {
                SelectObjects(tDetails.FoundInMaterials,ctrlPressed);
            }

            if(GUILayout.Button(tDetails.FoundInRenderers.Count+" GO",GUILayout.Width(50)))
            {
                List<UnityEngine.Object> FoundObjects=new List<UnityEngine.Object>();
                foreach (Renderer renderer in tDetails.FoundInRenderers) FoundObjects.Add(renderer.gameObject);
                SelectObjects(FoundObjects,ctrlPressed);
            }

            GUILayout.EndHorizontal();
        }
        if (ActiveTextures.Count>0)
        {
            GUILayout.BeginHorizontal ();
            GUILayout.Box(" ",GUILayout.Width(ThumbnailWidth),GUILayout.Height(ThumbnailHeight));

            if(GUILayout.Button("Select All",GUILayout.Width(150)))
            {
                List<UnityEngine.Object> AllTextures=new List<UnityEngine.Object>();
                foreach (TextureDetails tDetails in ActiveTextures) AllTextures.Add(tDetails.texture);
                SelectObjects(AllTextures,ctrlPressed);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    void ListMaterials()
    {
        materialListScrollPos = EditorGUILayout.BeginScrollView(materialListScrollPos);

        foreach (MaterialDetails tDetails in ActiveMaterials)
        {
            if (tDetails.material!=null)
            {
                GUILayout.BeginHorizontal ();

                if (tDetails.material.mainTexture!=null) GUILayout.Box(tDetails.material.mainTexture, GUILayout.Width(ThumbnailWidth), GUILayout.Height(ThumbnailHeight));
                else
                {
                    GUILayout.Box("n/a",GUILayout.Width(ThumbnailWidth),GUILayout.Height(ThumbnailHeight));
                }

                if(GUILayout.Button(tDetails.material.name,GUILayout.Width(150)))
                {
                    SelectObject(tDetails.material,ctrlPressed);
                }

                string shaderLabel = tDetails.material.shader != null ? tDetails.material.shader.name : "no shader";
                GUILayout.Label (shaderLabel, GUILayout.Width(200));

                if(GUILayout.Button(tDetails.FoundInRenderers.Count+" GO",GUILayout.Width(50)))
                {
                    List<UnityEngine.Object> FoundObjects=new List<UnityEngine.Object>();
                    foreach (Renderer renderer in tDetails.FoundInRenderers) FoundObjects.Add(renderer.gameObject);
                    SelectObjects(FoundObjects,ctrlPressed);
                }


                GUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void ListMeshes()
    {
        meshListScrollPos = EditorGUILayout.BeginScrollView(meshListScrollPos);

        foreach (MeshDetails tDetails in ActiveMeshDetails)
        {
            if (tDetails.mesh!=null)
            {
                GUILayout.BeginHorizontal ();
                /*
                if (tDetails.material.mainTexture!=null) GUILayout.Box(tDetails.material.mainTexture, GUILayout.Width(ThumbnailWidth), GUILayout.Height(ThumbnailHeight));
                else
                {
                    GUILayout.Box("n/a",GUILayout.Width(ThumbnailWidth),GUILayout.Height(ThumbnailHeight));
                }
                */

                if(GUILayout.Button(tDetails.mesh.name,GUILayout.Width(150)))
                {
                    SelectObject(tDetails.mesh,ctrlPressed);
                }
                string sizeLabel=""+tDetails.mesh.vertexCount+" vert";

                GUILayout.Label (sizeLabel,GUILayout.Width(100));


                if(GUILayout.Button(tDetails.FoundInMeshFilters.Count + " GO",GUILayout.Width(50)))
                {
                    List<UnityEngine.Object> FoundObjects=new List<UnityEngine.Object>();
                    foreach (MeshFilter meshFilter in tDetails.FoundInMeshFilters) FoundObjects.Add(meshFilter.gameObject);
                    SelectObjects(FoundObjects,ctrlPressed);
                }

                if(GUILayout.Button(tDetails.FoundInSkinnedMeshRenderer.Count + " GO",GUILayout.Width(50)))
                {
                    List<UnityEngine.Object> FoundObjects=new List<UnityEngine.Object>();
                    foreach (SkinnedMeshRenderer skinnedMeshRenderer in tDetails.FoundInSkinnedMeshRenderer) FoundObjects.Add(skinnedMeshRenderer.gameObject);
                    SelectObjects(FoundObjects,ctrlPressed);
                }


                GUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    string FormatSizeString(int memSizeKB)
    {
        if (memSizeKB<1024) return ""+memSizeKB+"k";
        else
        {
            float memSizeMB=((float)memSizeKB)/1024.0f;
            return memSizeMB.ToString("0.00")+"Mb";
        }
    }


    TextureDetails FindTextureDetails(Texture tTexture)
    {
        foreach (TextureDetails tTextureDetails in ActiveTextures)
        {
            if (tTextureDetails.texture==tTexture) return tTextureDetails;
        }
        return null;

    }

    MaterialDetails FindMaterialDetails(Material tMaterial)
    {
        foreach (MaterialDetails tMaterialDetails in ActiveMaterials)
        {
            if (tMaterialDetails.material==tMaterial) return tMaterialDetails;
        }
        return null;

    }

	ImageDetails FindImageDetails(Image image)
	{
		foreach (ImageDetails imgDetails in ActiveImages)
		{
			if (imgDetails.image.mainTexture==image.mainTexture) return imgDetails;
		}
		return null;

	}

    MeshDetails FindMeshDetails(Mesh tMesh)
    {
        foreach (MeshDetails tMeshDetails in ActiveMeshDetails)
        {
            if (tMeshDetails.mesh==tMesh) return tMeshDetails;
        }
        return null;

    }

    bool InAtlas(Sprite sprite)
    {
        if (sprite.packed)
        {
            return true;
        }

        foreach (var atlas in _spriteAtlases)
        {
            if (atlas.CanBindTo(sprite))
            {
                return true;
            }
        }

        return false;
    }

    void CheckResources()
    {
        ActiveTextures.Clear();
        ActiveMaterials.Clear();
        ActiveMeshDetails.Clear();
		ActiveImages.Clear ();


        var guids = AssetDatabase.FindAssets("t:SpriteAtlas", null);
        _spriteAtlases = new List<SpriteAtlas>(guids.Length);

        foreach (var guid in guids)
        {
            _spriteAtlases.Add(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid)));
        }

		Image[] images = Resources.FindObjectsOfTypeAll<Image> ();

		foreach (Image img in images)
		{
			if(img.sprite != null)
			{
				ImageDetails imgDetailFound = FindImageDetails (img);
				if (imgDetailFound == null) {

					ImageDetails imgDetail = new ImageDetails ();
					imgDetail.image = img;
					imgDetail.packed = InAtlas(img.sprite);
					imgDetail.format = (img.mainTexture as Texture2D).format;

					imgDetail.FoundInObjects.Add (img);

					ActiveImages.Add (imgDetail);
				}
				else
				{
					imgDetailFound.FoundInObjects.Add (img);
				}
			}
		}

        Renderer[] renderers = (Renderer[]) FindObjectsOfType(typeof(Renderer));
        //Debug.Log("Total renderers "+renderers.Length);
        foreach (Renderer renderer in renderers)
        {
            //Debug.Log("Renderer is "+renderer.name);
            foreach (Material material in renderer.sharedMaterials)
            {

                MaterialDetails tMaterialDetails=FindMaterialDetails(material);
                if (tMaterialDetails==null)
                {
                    tMaterialDetails=new MaterialDetails();
                    tMaterialDetails.material=material;
                    ActiveMaterials.Add(tMaterialDetails);
                }
                tMaterialDetails.FoundInRenderers.Add(renderer);
            }
        }

        foreach (MaterialDetails tMaterialDetails in ActiveMaterials)
        {
            Material tMaterial=tMaterialDetails.material;

            if(tMaterial == null)
                continue;

            var dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] {tMaterial});
            foreach (UnityEngine.Object obj in dependencies)
            {
                if (obj is Texture)
                {
                    Texture tTexture=obj as Texture;
                    var tTextureDetail = GetTextureDetail(tTexture, tMaterial, tMaterialDetails);
                    ActiveTextures.Add(tTextureDetail);
                }
            }

            //if the texture was downloaded, it won't be included in the editor dependencies
            if (tMaterial.mainTexture != null && !dependencies.Contains(tMaterial.mainTexture))
            {
                var tTextureDetail = GetTextureDetail(tMaterial.mainTexture, tMaterial, tMaterialDetails);
                ActiveTextures.Add(tTextureDetail);
            }
        }


        MeshFilter[] meshFilters = (MeshFilter[]) FindObjectsOfType(typeof(MeshFilter));

        foreach (MeshFilter tMeshFilter in meshFilters)
        {
            Mesh tMesh=tMeshFilter.sharedMesh;
            if (tMesh!=null)
            {
                MeshDetails tMeshDetails=FindMeshDetails(tMesh);
                if (tMeshDetails==null)
                {
                    tMeshDetails=new MeshDetails();
                    tMeshDetails.mesh=tMesh;
                    ActiveMeshDetails.Add(tMeshDetails);
                }
                tMeshDetails.FoundInMeshFilters.Add(tMeshFilter);
            }
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = (SkinnedMeshRenderer[]) FindObjectsOfType(typeof(SkinnedMeshRenderer));

        foreach (SkinnedMeshRenderer tSkinnedMeshRenderer in skinnedMeshRenderers)
        {
            Mesh tMesh=tSkinnedMeshRenderer.sharedMesh;
            if (tMesh!=null)
            {
                MeshDetails tMeshDetails=FindMeshDetails(tMesh);
                if (tMeshDetails==null)
                {
                    tMeshDetails=new MeshDetails();
                    tMeshDetails.mesh=tMesh;
                    ActiveMeshDetails.Add(tMeshDetails);
                }
                tMeshDetails.FoundInSkinnedMeshRenderer.Add(tSkinnedMeshRenderer);
            }
        }


        TotalTextureMemory=0;
        foreach (TextureDetails tTextureDetails in ActiveTextures) TotalTextureMemory+=tTextureDetails.memSizeKB;

        TotalMeshVertices=0;
        foreach (MeshDetails tMeshDetails in ActiveMeshDetails) TotalMeshVertices+=tMeshDetails.mesh.vertexCount;

        // Sort by size, descending
        ActiveTextures.Sort(delegate(TextureDetails details1, TextureDetails details2) {return details2.memSizeKB-details1.memSizeKB;});
        ActiveMeshDetails.Sort(delegate(MeshDetails details1, MeshDetails details2) {return details2.mesh.vertexCount-details1.mesh.vertexCount;});

    }

    private TextureDetails GetTextureDetail(Texture tTexture, Material tMaterial, MaterialDetails tMaterialDetails)
    {
        TextureDetails tTextureDetails = FindTextureDetails(tTexture);
        if (tTextureDetails == null)
        {
            tTextureDetails = new TextureDetails();
            tTextureDetails.texture = tTexture;
            tTextureDetails.isCubeMap = tTexture is Cubemap;

            int memSize = CalculateTextureSizeBytes(tTexture);

            tTextureDetails.memSizeKB = memSize / 1024;
            TextureFormat tFormat = TextureFormat.RGBA32;
            int tMipMapCount = 1;
            if (tTexture is Texture2D)
            {
                tFormat = (tTexture as Texture2D).format;
                tMipMapCount = (tTexture as Texture2D).mipmapCount;
            }
            if (tTexture is Cubemap)
            {
                tFormat = (tTexture as Cubemap).format;
            }

            tTextureDetails.format = tFormat;
            tTextureDetails.mipMapCount = tMipMapCount;

        }
        tTextureDetails.FoundInMaterials.Add(tMaterial);
        foreach (Renderer renderer in tMaterialDetails.FoundInRenderers)
        {
            if (!tTextureDetails.FoundInRenderers.Contains(renderer)) tTextureDetails.FoundInRenderers.Add(renderer);
        }
        return tTextureDetails;
    }

}
