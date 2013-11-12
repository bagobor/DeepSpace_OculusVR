using UnityEngine;
using System;
using System.Collections;

[AddComponentMenu("2D Toolkit/Sprite/tk2dSprite")]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
/// <summary>
/// Sprite implementation which maintains its own Unity Mesh. Leverages dynamic batching.
/// </summary>
public class tk2dSprite : tk2dBaseSprite
{
    private const float MIN_Y_VALUE = 0.0f;
    private const float MAX_Y_VALUE = 160.0f;
    private const float MIN_Z_VALUE = -200.0f;
    private const float MAX_Z_VALUE = 50.0f;
    private const float MIN_X_VALUE = 0.0f;
    private const float MAX_X_VALUE = 60.0f;
    private const float LERP_Z_DENOM = 1.0f / (MIN_Z_VALUE - MAX_Z_VALUE);
    private const float LERP_X_DENOM = 0.0f / (MIN_X_VALUE - MAX_X_VALUE);
    
    Mesh mesh;
	Vector3[] meshVertices;
	Vector3[] meshNormals = null;
	Vector4[] meshTangents = null;
	Color[] meshColors;
        
    private Transform mTransform;
    
    public ZSpriteManager mZSpriteManager = null;
    public ZSprite mZSprite = null;
    public Vector3 mZPreviousPosition;
    
	new void Awake()
	{
		base.Awake();
        mTransform = transform;
	}
	
    void OnEnable()
    {
        if (Application.isPlaying && tk2dSystem.useZSpriteManager)
        {
            if (mZSpriteManager == null)
            {
                mZSpriteManager = ZSpriteManager.DefaultSpriteManager;
            }
        }
        
        if (mZSpriteManager == null)
        {
            // Create mesh, independently to everything else
            mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            GetComponent<MeshFilter>().mesh = mesh;
        }
     
        // This will not be set when instantiating in code
        // In that case, Build will need to be called
        if (Collection)
        {
            // reset spriteId if outside bounds
            // this is when the sprite collection data is corrupt
            if (_spriteId < 0 || _spriteId >= Collection.Count)
                _spriteId = 0;
            
            Build();
        }
    }
    
    void OnDisable()
    {
        if (mZSprite != null)
        {
            mZSpriteManager.RemoveSprite(mZSprite);
            mZSprite = null;
        }        
    }
    
	protected void OnDestroy()
	{
		if (mesh)
		{
#if UNITY_EDITOR
			DestroyImmediate(mesh);
#else
			Destroy(mesh);
#endif
		}
		
        if (meshColliderMesh)
		{
#if UNITY_EDITOR
			DestroyImmediate(meshColliderMesh);
#else
			Destroy(meshColliderMesh);
#endif
		}
	}
	
	public override void Build()
	{
        if (mZSpriteManager == null)
        {
            var sprite = collectionInst.spriteDefinitions[spriteId];

            meshVertices = new Vector3[sprite.positions.Length];
            meshColors = new Color[sprite.positions.Length];
            
            meshNormals = new Vector3[0];
            meshTangents = new Vector4[0];
            
            if (sprite.normals != null && sprite.normals.Length > 0)
            {
                meshNormals = new Vector3[sprite.normals.Length];
            }
            if (sprite.tangents != null && sprite.tangents.Length > 0)
            {
                meshTangents = new Vector4[sprite.tangents.Length];
            }
            
            SetPositions(meshVertices, meshNormals, meshTangents);
            SetColors(meshColors);
            
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.hideFlags = HideFlags.DontSave;
                GetComponent<MeshFilter>().mesh = mesh;
            }
            
            mesh.Clear();
            mesh.vertices = meshVertices;
            mesh.normals = meshNormals;
            mesh.tangents = meshTangents;
            mesh.colors = meshColors;
            mesh.uv = sprite.uvs;
            mesh.triangles = sprite.indices;        
            
            UpdateMaterial();
            CreateCollider();
        }
        else
        {
            if (mZSprite == null)
            {
                mZSprite = mZSpriteManager.CreateSprite();
            }
   
            renderer.enabled = false;            
            SetZSpritePosition();
            UpdateMaterial();
            CreateCollider();
        }	
	}
	
	/// <summary>
	/// Adds a tk2dSprite as a component to the gameObject passed in, setting up necessary parameters and building geometry.
	/// Convenience alias of tk2dBaseSprite.AddComponent<tk2dSprite>(...).
	/// </summary>
	public static tk2dSprite AddComponent(GameObject go, tk2dSpriteCollectionData spriteCollection, int spriteId)
	{
		return tk2dBaseSprite.AddComponent<tk2dSprite>(go, spriteCollection, spriteId);
	}
	
	/// <summary>
	/// Create a sprite (and gameObject) displaying the region of the texture specified.
	/// Use <see cref="tk2dSpriteCollectionData.CreateFromTexture"/> if you need to create a sprite collection
	/// with multiple sprites.
	/// Convenience alias of tk2dBaseSprite.CreateFromTexture<tk2dSprite>(...)
	/// </summary>
	public static GameObject CreateFromTexture(Texture2D texture, tk2dRuntime.SpriteCollectionSize size, Rect region, Vector2 anchor)
	{
		return tk2dBaseSprite.CreateFromTexture<tk2dSprite>(texture, size, region, anchor);
	}

	protected override void UpdateGeometry() { UpdateGeometryImpl(); }
	protected override void UpdateColors() { UpdateColorsImpl(); }
	protected override void UpdateVertices() { UpdateVerticesImpl(); }
	
	
	protected void UpdateColorsImpl()
	{
#if UNITY_EDITOR
		// This can happen with prefabs in the inspector
		if ((mesh == null || meshColors == null || meshColors.Length == 0) && (mZSprite == null))
			return;
#endif
		
        if (mZSprite == null)
        {
            SetColors(meshColors);
            mesh.colors = meshColors;
        }
        else
        {
            Color32 c = _color;
            if (collectionInst.premultipliedAlpha) 
            { 
                c.r *= c.a; 
                c.g *= c.a; 
                c.b *= c.a; 
            }
            mZSprite.color = c;
        }
	}
	
	protected void UpdateVerticesImpl()
	{
#if UNITY_EDITOR
		// This can happen with prefabs in the inspector
		if ((mesh == null || meshVertices == null || meshVertices.Length == 0) && mZSprite == null)
			return;
#endif
		
        if (mZSprite == null)
        {
            var sprite = collectionInst.spriteDefinitions[spriteId];
     
            // Clear out normals and tangents when switching from a sprite with them to one without
            if (sprite.normals.Length != meshNormals.Length)
            {
                meshNormals = (sprite.normals != null && sprite.normals.Length > 0)?(new Vector3[sprite.normals.Length]):(new Vector3[0]);
            }
            if (sprite.tangents.Length != meshTangents.Length)
            {
                meshTangents = (sprite.tangents != null && sprite.tangents.Length > 0)?(new Vector4[sprite.tangents.Length]):(new Vector4[0]);
            }
            
            SetPositions(meshVertices, meshNormals, meshTangents);
            mesh.vertices = meshVertices;
            mesh.normals = meshNormals;
            mesh.tangents = meshTangents;
            mesh.uv = sprite.uvs;
            mesh.bounds = GetBounds();
        }
        else
        {
            SetZSpritePosition();
        }
	}

	protected void UpdateGeometryImpl()
	{
#if UNITY_EDITOR
		// This can happen with prefabs in the inspector
		if (mesh == null && mZSprite == null)
			return;
#else
		if (mesh == null && mZSprite == null)
			Build();
#endif
		
        if (mZSprite == null)
        {
            var sprite = collectionInst.spriteDefinitions[spriteId];
            if (meshVertices == null || meshVertices.Length != sprite.positions.Length)
            {
                meshVertices = new Vector3[sprite.positions.Length];
                meshNormals = (sprite.normals != null && sprite.normals.Length > 0)?(new Vector3[sprite.normals.Length]):(new Vector3[0]);
                meshTangents = (sprite.tangents != null && sprite.tangents.Length > 0)?(new Vector4[sprite.tangents.Length]):(new Vector4[0]);
                meshColors = new Color[sprite.positions.Length];
            }
            SetPositions(meshVertices, meshNormals, meshTangents);
            SetColors(meshColors);
            
            mesh.Clear();
            mesh.vertices = meshVertices;
            mesh.normals = meshNormals;
            mesh.tangents = meshTangents;
            mesh.colors = meshColors;
            mesh.uv = sprite.uvs;
            mesh.bounds = GetBounds();
            mesh.triangles = sprite.indices;
        }
        else
        {
            SetZSpritePosition();
        }
	}
    
    void SetZSpritePosition()
    {
        var sprite = collectionInst.spriteDefinitions[spriteId];
        Vector3 p = transform.position;
        Matrix4x4 m = transform.localToWorldMatrix;
        Color32 c = _color;
        if (collectionInst.premultipliedAlpha) 
        { 
            c.r *= c.a; 
            c.g *= c.a; 
            c.b *= c.a; 
        }
        
        float depth = (p.z - MAX_Z_VALUE) * LERP_Z_DENOM;
        if (depth < 0)
        {
            depth = 0;
        }
        else if (depth > 1.0f)
        {
            depth = 1.0f;
        }
        
        mZSprite.vertices[0].x = sprite.positions[0].x * _scale.x;
        mZSprite.vertices[0].y = sprite.positions[0].y * _scale.y;
        mZSprite.vertices[1].x = sprite.positions[1].x * _scale.x;
        mZSprite.vertices[1].y = sprite.positions[1].y * _scale.y;
        mZSprite.vertices[2].x = sprite.positions[2].x * _scale.x;
        mZSprite.vertices[2].y = sprite.positions[2].y * _scale.y;
        mZSprite.vertices[3].x = sprite.positions[3].x * _scale.x;
        mZSprite.vertices[3].y = sprite.positions[3].y * _scale.y;
        mZSprite.vertices[0] = m.MultiplyPoint3x4(mZSprite.vertices[0]);
        mZSprite.vertices[1] = m.MultiplyPoint3x4(mZSprite.vertices[1]);
        mZSprite.vertices[2] = m.MultiplyPoint3x4(mZSprite.vertices[2]);
        mZSprite.vertices[3] = m.MultiplyPoint3x4(mZSprite.vertices[3]);
        mZSprite.vertices[0].z = p.z;
        mZSprite.vertices[1].z = p.z;
        mZSprite.vertices[2].z = p.z;
        mZSprite.vertices[3].z = p.z;
        mZSprite.bucket = (UInt16)(depth * (mZSpriteManager.NumBuckets - 1));
        mZPreviousPosition = p;
    }
    
    void LateUpdate()
    {
        if (mZSprite != null)
        {
            Vector3 p = mTransform.position;
            if (p.x != mZPreviousPosition.x || p.y != mZPreviousPosition.y || p.z != mZPreviousPosition.z)
            {
                SetZSpritePosition();
            }
        }
    }
    
	protected override void UpdateMaterial()
	{
        if (mZSprite == null)
        {
            if (renderer.sharedMaterial != collectionInst.spriteDefinitions[spriteId].materialInst)
                renderer.material = collectionInst.spriteDefinitions[spriteId].materialInst;
        }
        else
        {
            tk2dSpriteDefinition sprite = collectionInst.spriteDefinitions[spriteId];
            Material material = sprite.materialInst;
            if (mZSprite.material == null)
            {
                mZSprite.material = mZSpriteManager.AddMaterial(material);
            }
            else if (mZSprite.material.material != material)
            {
                mZSpriteManager.ReleaseMaterial(mZSprite.material);
                mZSprite.material = mZSpriteManager.AddMaterial(material);
            }
            mZSprite.uvs[0] = sprite.uvs[0];
            mZSprite.uvs[1] = sprite.uvs[1];
            mZSprite.uvs[2] = sprite.uvs[2];
            mZSprite.uvs[3] = sprite.uvs[3];
            mZSprite.color = color;
        }
	}
	
	protected override int GetCurrentVertexCount()
	{
#if UNITY_EDITOR
		if (meshVertices == null)
			return 0;
#else
		if (meshVertices == null && mZSprite == null)
			Build();
#endif
		// Really nasty bug here found by Andrew Welch.
		return mZSprite == null ? meshVertices.Length : mZSprite.vertices.Length;
	}
	
	public override void ForceBuild()
	{
		base.ForceBuild();
		GetComponent<MeshFilter>().mesh = mesh;
	}
}
