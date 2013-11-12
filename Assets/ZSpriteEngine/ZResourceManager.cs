using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;


// spotco -- todo -- kill all the asset downloading code
public interface IResourceManager
{
    Object Load(string path);
    Object Load(string path, System.Type systemTypeInstance);
    Object LoadAssetAtPath(string path, System.Type systemTypeInstance);
}

public class ZResourceManager : SingletonMonoBehaviour<ZResourceManager>, IResourceManager
{
    public const string ASSET_REPO_URL = "";
    
    private List<AssetBundle> loadedBundles = new List<AssetBundle>();
    private List<ZAssetBundle> allBundles = new List<ZAssetBundle>();
 
    public List<ZAssetBundle> AllBundles {get {return allBundles;}}
    
    IEnumerator Start() {    
			yield return true;
    }
           
    protected override void Awake()
    {
        base.Awake();
    }
    
    public static Object SafeLoad(string path)
    {
        if (ZResourceManager.DoesInstanceExist())
        {
            return Instance.Load (path);
        }
        else
        {
            return Resources.Load (path);
        }
    }
    
    public static Object SafeLoadAssetAtPath(string path, System.Type systemTypeInstance)
    {
        if (ZResourceManager.DoesInstanceExist())
        {
            return Instance.LoadAssetAtPath(path, systemTypeInstance);
        }
        else
        {
            return Resources.LoadAssetAtPath(path, systemTypeInstance);
        }
    }
    
    public Object Load(string path)
    {
        return Load (path, null);
    }
 
    public Object Load(string path, System.Type type)
    {
        Object resource;
        resource = LoadFromResources(path, type);
        if (resource == null)
        {
            resource = LoadFromAssetBundles(path);
        } else 
        {
        }
        return resource;
    }

    public Object LoadAssetAtPath(string path, System.Type type)
    {
        Object resource;
        resource = LoadAssetAtPathFromResources(path, type);
        if (resource == null)
        {
            resource = LoadFromAssetBundles(path);
        } else 
        {
        }
        return resource;    
    }
    
    private Object LoadFromResources(string path, System.Type type)
    {
        Object result;
        try 
        {
            if (type != null)
            {
                result = Resources.Load(path, type);
            } 
            else
            {
                result = Resources.Load(path);
            }
        } 
        catch (System.Exception) 
        {
            result = null;
        }
        return result;
    }

    private Object LoadAssetAtPathFromResources(string path, System.Type type)
    {
        Object result;
        try 
        {
            result = Resources.LoadAssetAtPath(path, type);
        } 
        catch (System.Exception) 
        {
            result = null;
        }
        return result;
    }

    private Object LoadFromAssetBundles(string path)
    {
        string assetName = System.IO.Path.GetFileName(path);  
        foreach (AssetBundle bundle in loadedBundles) 
        {
            try 
            {
                if( bundle != null )
                {
                    Object asset = bundle.Load(assetName);
                    if (asset != null)
                    {
                        Debug.Log("Loaded from asset bundle: " + path);
                        return asset;
                    }
                } 
            }
            catch (System.Exception e)
            {
                Debug.LogError("LoadFromAssetBundles: " + e);
            }
        }
        
        return null;    
    }
       
    private ZResourceManager() 
    {
        InitBundles();
    }
    
    private void InitBundles()
    {
        // Audio Bundle
        allBundles.Add(
            new ZAssetBundle(
                1, // version
                "sounds", // bundle name
                OnSoundsLoaded, // on load callback
                GetAudioFilesToExclude(), // files to exclude from the build
                delegate() {return Resources.FindObjectsOfTypeAll(typeof(AudioClip));} // Objects for bundle
            )
        );
    }
    
    private string[] GetAudioFilesToExclude()
    {
		string[] rtv = {""};
		return rtv;
    }
    
    public void OnSoundsLoaded() 
    {
		//spotco -- log here
    }    

    public void OnFireHeroLoaded() 
    {
    }    
}

public class ZAssetBundle 
{
    private int _version;
    private System.Action _onLoadCallback;
    private string _name;
    private string[] _files;
    private System.Func<Object[]> _getObjectsForBundle;
    
    public ZAssetBundle(int version, string name, System.Action onLoadCallback, string[] files, System.Func<Object[]> getObjectsForBundle)
    {
        _version = version;
        _onLoadCallback = onLoadCallback;
        _name = name;
        _files = files;
        _getObjectsForBundle = getObjectsForBundle;
    }

    public int Version { get { return _version; } }
    
    public string Name { get { return _name; } }
    
    public System.Action OnLoadCallback { get { return _onLoadCallback; } }
    
    public string[] Files { get { return _files; } }
    
    public Object[] ObjectsForBundle { get { return _getObjectsForBundle(); } }

    public string Url 
    { 
        get 
        { 
            string platform = "editor";
#if UNITY_IPHONE
            platform = "ios";
#endif            
#if UNITY_ANDROID
            platform = "android";
#endif            
            return ZResourceManager.ASSET_REPO_URL + platform + "/" + _name;
        } 
    }     
	
	public string PathToSave
	{
        get 
        { 
            string platform = "editor";
#if UNITY_IPHONE
            platform = "ios";
#endif            
#if UNITY_ANDROID
            platform = "android";
#endif            
            // http://assets-dev-hero.hero-dev-01.zc1.zynga.com/ios/sounds
            return "Assets/bundles/" + platform + "/" + _name;
        } 
	}
}
