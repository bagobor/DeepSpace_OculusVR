//#define LOG_DEBUG_STATS
using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class ZSprite
{
	public int id = -1;
	public int bucket = 0; // This will not be validated for performance reasons so make sure it is a valid bucket index
    public Color32 color = new Color32(255, 255, 255, 255);
	public ZSpriteMaterial material = null;
	public Vector2[] uvs = new Vector2[4];
	public Vector3[] vertices = new Vector3[4];
    public Color32[] colors = new Color32[4];
}

public class ZSpriteMaterial
{
	public int id;
	public int refCount;
	public Material material;
}

public class ZSpriteManager : MonoBehaviour 
{	
    public static ZSpriteManager DefaultSpriteManager;
    public int NumBuckets = 1 << 16;
    public int MaxSpritesPerBucket = 32;
    public int NumReservedSprites = 512;
    public int MaxSpritesPerBatch = 128;
    public Camera Camera;
    public bool UseAsDefault = false;
    
	private int mNumBuckets = 0;
	private int mMaxSpritesPerBucket = 0;
	private int mNumAllocatedSprites = 0;
	private int mNumUsedSprites = 0;
	private ZSprite[] mSprites;
	
	private int mNumAllocatedMaterials = 0;
	private int mNumUsedMaterials = 0;
	private ZSpriteMaterial[] mMaterials = new ZSpriteMaterial[0];

	private int[] mBuckets;
	private int[] mBucketCounts;
    private int[] mBatchSprites;
    private int[] mBatchCounts;
    private int mNumBatches;
    private Mesh[] mMeshes;
	private Vector3[][] mVertices;
	private Vector2[][] mUVs;
    private Color32[][] mColors;
	private int[][] mTriangles;
    private Vector3 mZeroVector = Vector3.zero;
    private Quaternion mIdentityQuaternion = Quaternion.identity;
    
    public int NumAllocatedSprites { get { return mNumAllocatedSprites; } }
	public int NumUsedSprites { get { return mNumUsedSprites; } }
	
	void OnEnable()
	{
        if (UseAsDefault)
        {
            DefaultSpriteManager = this;
        }

        if (mBuckets == null)
		{
			SetupBuckets(NumBuckets, MaxSpritesPerBucket);
		}
		
		if (mSprites == null)
		{
			ReserveSpriteMemory(NumReservedSprites);
		}		
	}
	
	public void SetupBuckets(int numBuckets, int maxSpritesPerBucket)
	{
		if (mBuckets == null || numBuckets != mNumBuckets || maxSpritesPerBucket != mMaxSpritesPerBucket)
		{
			mNumBuckets = numBuckets;
			mMaxSpritesPerBucket = maxSpritesPerBucket;
			mBuckets = new int[mNumBuckets * mMaxSpritesPerBucket];
			mBucketCounts = new int[mNumBuckets];
		}
	}
	
	public void ReserveSpriteMemory(int numSprites)
	{
		int prevNumAllocatedSprites = mNumAllocatedSprites;
		
		if (mSprites == null)
		{
			mSprites = new ZSprite[numSprites];
			mBatchSprites = new int[numSprites];
            mBatchCounts = new int [numSprites];
			mVertices = new Vector3[numSprites][];
			mUVs = new Vector2[numSprites][];
            mColors = new Color32[numSprites][];
			mTriangles = new int[numSprites][];
            mMeshes = new Mesh[numSprites];
			mNumAllocatedSprites = numSprites;
		}
		else if (prevNumAllocatedSprites < numSprites)
		{
			Array.Resize(ref mSprites, numSprites);
			Array.Resize(ref mVertices, numSprites);
			Array.Resize(ref mUVs, numSprites);
            Array.Resize(ref mColors, numSprites);
			Array.Resize(ref mTriangles, numSprites);
            Array.Resize(ref mMeshes, numSprites);
			mBatchSprites = new int[numSprites];
            mBatchCounts = new int[numSprites];
			mNumAllocatedSprites = numSprites;
		}
		
		for (int i = prevNumAllocatedSprites; i < mNumAllocatedSprites; i++)
		{
			mSprites[i] = new ZSprite();
			mSprites[i].id = i;
			
			if (i > MaxSpritesPerBatch)
			{
				continue;
			}
			
			int numSpritesInBatch = i;
			mVertices[numSpritesInBatch] = new Vector3[numSpritesInBatch * 4];
			mUVs[numSpritesInBatch] = new Vector2[numSpritesInBatch * 4];
            mColors[numSpritesInBatch] = new Color32[numSpritesInBatch * 4];
			mTriangles[numSpritesInBatch] = new int[numSpritesInBatch * 6];

            for (int n = 0; n < i; n++)
			{
				int iOffset = n * 6;
				int vOffset = n * 4;
                mTriangles[numSpritesInBatch][iOffset] = vOffset;
				mTriangles[numSpritesInBatch][iOffset + 1] = vOffset + 3;
				mTriangles[numSpritesInBatch][iOffset + 2] = vOffset + 1;
				mTriangles[numSpritesInBatch][iOffset + 3] = vOffset + 2;
				mTriangles[numSpritesInBatch][iOffset + 4] = vOffset + 3;
				mTriangles[numSpritesInBatch][iOffset + 5] = vOffset;
			}
			
			mMeshes[numSpritesInBatch] = new Mesh();
            mMeshes[numSpritesInBatch].MarkDynamic();
            mMeshes[numSpritesInBatch].vertices = mVertices[numSpritesInBatch];
            mMeshes[numSpritesInBatch].uv = mUVs[numSpritesInBatch];
            mMeshes[numSpritesInBatch].colors32 = mColors[numSpritesInBatch];
            mMeshes[numSpritesInBatch].triangles = mTriangles[numSpritesInBatch];
		}		
	}
	
	public ZSprite CreateSprite()
	{
		if (mNumAllocatedSprites == mNumUsedSprites)
		{
			ReserveSpriteMemory(mNumAllocatedSprites * 2);
		}
		
		ZSprite sprite = mSprites[mNumUsedSprites++];
		return sprite;
	}
		
	public void RemoveSprite(ZSprite sprite)
	{
		if (sprite == null || sprite.id < 0 || sprite.id >= mNumUsedSprites || mSprites[sprite.id] != sprite)
		{
			return;
		}
		
        // Swap the last sprite with the removed one
		mNumUsedSprites--;
		mSprites[sprite.id] = mSprites[mNumUsedSprites];
		mSprites[sprite.id].id = sprite.id;
		mSprites[mNumUsedSprites] = sprite;

        ReleaseMaterial(sprite.material);
		sprite.id = mNumUsedSprites;
        sprite.material = null;
	}
	
	public ZSpriteMaterial AddMaterial(Material material)
	{
		ZSpriteMaterial materialRef = Array.Find(mMaterials, m => m.material == material);
		if (materialRef == null)
		{
			if (mNumUsedMaterials == mNumAllocatedMaterials)
			{
				if (mNumAllocatedMaterials == 0)
				{
					mNumAllocatedMaterials = 1;
				}
				else
				{
					mNumAllocatedMaterials *= 2;
				}
				
				Array.Resize(ref mMaterials, mNumAllocatedMaterials);
				
				for (int i = mNumUsedMaterials; i < mNumAllocatedMaterials; i++)
				{
					mMaterials[i] = new ZSpriteMaterial();
					mMaterials[i].id = i;
				}
			}
			
			materialRef = mMaterials[mNumUsedMaterials++];
			materialRef.material = material;
		}
		
		return RetainMaterial(materialRef);
	}
	
	public ZSpriteMaterial RetainMaterial(ZSpriteMaterial materialRef)
	{
		if (materialRef != null)
		{
			materialRef.refCount++;
		}
		
		return materialRef;
	}
	
	public void ReleaseMaterial(ZSpriteMaterial materialRef)
	{
		if (materialRef == null || materialRef.id < 0 || materialRef.id >= mNumUsedMaterials || mMaterials[materialRef.id] != materialRef)
		{
			return;
		}
		
		materialRef.refCount--;
		if (materialRef.refCount <= 0)
		{
            // Swap the last material ref with the removed one
			mNumUsedMaterials--;
			mMaterials[materialRef.id] = mMaterials[mNumUsedMaterials];
            mMaterials[materialRef.id].id = materialRef.id;	
            mMaterials[mNumUsedMaterials] = materialRef;
			materialRef.id = mNumUsedMaterials;
			materialRef.refCount = 0;
			materialRef.material = null;
		}		
	}
	
    public void OnDisable()
    {
        // Release all materials
        while (mNumUsedMaterials > 0)
        {
            mNumUsedMaterials--;
            mMaterials[mNumUsedMaterials].refCount = 0;
            mMaterials[mNumUsedMaterials].material = null;
        }
        
        // Release all sprites
        while (mNumUsedSprites > 0)
        {
            mNumUsedSprites--;
            mSprites[mNumUsedSprites].material = null;
        }
    }
 
    void LateUpdate()
    {
        CreateBatches();
    }
    
    void CreateBatches()
    {
		// Put the sprites in buckets instead of true sorting
		// Hopefully, we can even speed this up later by not rebuilding the buckets every frame
        // Have to figure out how to be flexible though without actually making a lot of function calls
        // Maybe the stats will show that we don't actually update the bucket very much?
		for (int i = 0; i < mNumUsedSprites; i++)
		{
			ZSprite sprite = mSprites[i];
			int bucketIndex = sprite.bucket;
			int bucketCount = mBucketCounts[bucketIndex];
			if (bucketCount < mMaxSpritesPerBucket)
			{
				mBuckets[bucketIndex * mMaxSpritesPerBucket + bucketCount] = i;
				mBucketCounts[bucketIndex]++;
			}
		}   
    
#if LOG_DEBUG_STATS
        int maxInBucket = 0;
        string debugStats = "ZSpriteManager::OnRenderObject " + Time.time;
#endif
        
        int numSprites = 0;
		
		for (int i = 0; i < mNumBuckets; i++)
		{
#if LOG_DEBUG_STATS
            if (mBucketCounts[i] > maxInBucket)
            {
                maxInBucket = mBucketCounts[i];
            }
#endif
			int bucketBegin = i * mMaxSpritesPerBucket;
			int bucketEnd = bucketBegin + mBucketCounts[i];
			mBucketCounts[i] = 0;
			
			for (int bucketIndex = bucketBegin; bucketIndex < bucketEnd; bucketIndex++)
			{
				int spriteIndex = mBuckets[bucketIndex];
                mBatchSprites[numSprites++] = spriteIndex;
            }
        }
        
        mNumBatches = 0;
        int material = -1;
        int numSpritesInBatch = 0;
        for (int i = 0; i < numSprites; i++)
        {
			ZSprite sprite = mSprites[mBatchSprites[i]];
			if (sprite.material.id == material && numSpritesInBatch < MaxSpritesPerBatch)
			{
                numSpritesInBatch++;
			}
			else
			{
				if (numSpritesInBatch > 0)
				{
                    mBatchCounts[mNumBatches] = numSpritesInBatch;
                    mNumBatches++;
#if LOG_DEBUG_STATS
                    debugStats += String.Format(" {0}:{1}", material, numSpritesInBatch);
#endif          
				}

				material = sprite.material.id;
				numSpritesInBatch = 1;
			}
		}
		
		if (numSpritesInBatch > 0)
		{
            mBatchCounts[mNumBatches] = numSpritesInBatch;
            mNumBatches++;
            
#if LOG_DEBUG_STATS
            debugStats += String.Format(" {0}:{1}", id, numSpritesInBatch);
#endif       
		}
        
#if LOG_DEBUG_STATS
        Debug.Log(String.Format("{0}\nNumBatches = {1} MaxInBucket = {2}", debugStats, numBatches, maxInBucket));
#endif
    }
    
	void OnRenderObject() 
	{
        Camera camera = Camera == null ? UnityEngine.Camera.main : Camera; 

/*
#if UNITY_EDITOR
        if (UnityEngine.Camera.current != camera && Array.IndexOf(UnityEditor.SceneView.GetAllSceneCameras(), UnityEngine.Camera.current) == -1)
        {
            return;
        }
#else
*/
        if (UnityEngine.Camera.current != camera)
        {
            return;
        }
//#endif
        
        //CreateBatches();
        
        int spriteIndex = 0;
        for (int i = 0; i < mNumBatches; i++)
        {
            int numSpritesInBatch = mBatchCounts[i];
            Vector3[] vertices = mVertices[numSpritesInBatch];
    		Vector2[] uvs = mUVs[numSpritesInBatch];
            Color32[] colors = mColors[numSpritesInBatch];
            Material material = mSprites[mBatchSprites[spriteIndex]].material.material;
        
            int numElements = numSpritesInBatch * 4;
            for (int vOffset = 0; vOffset < numElements; vOffset += 4)
            {
    			ZSprite batchedSprite = mSprites[mBatchSprites[spriteIndex++]];
    			vertices[vOffset] = batchedSprite.vertices[0];
    			vertices[vOffset + 1] = batchedSprite.vertices[1];
    			vertices[vOffset + 2] = batchedSprite.vertices[2];
    			vertices[vOffset + 3] = batchedSprite.vertices[3];
    			uvs[vOffset] = batchedSprite.uvs[0];
    			uvs[vOffset + 1] = batchedSprite.uvs[1];
    			uvs[vOffset + 2] = batchedSprite.uvs[2];
    			uvs[vOffset + 3] = batchedSprite.uvs[3];
                colors[vOffset] = batchedSprite.color;
                colors[vOffset + 1] = batchedSprite.color;
                colors[vOffset + 2] = batchedSprite.color;
                colors[vOffset + 3] = batchedSprite.color;
    		}
			
            Mesh mesh = mMeshes[numSpritesInBatch];
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors32 = colors;

            material.SetPass(0);    
            Graphics.DrawMeshNow(mesh, mZeroVector, mIdentityQuaternion);            
        }
    }
}
