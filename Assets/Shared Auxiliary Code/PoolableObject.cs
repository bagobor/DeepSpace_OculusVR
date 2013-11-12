/*************************************************************
 *           Unity Object Pool (c) by ClockStone 2011        *
 * 
 * Allows to "pool" prefab objects to avoid large number of
 * Instantiate() calls.
 * 
 * Usage:
 * 
 * Add the PoolableObject script component to the prefab to be pooled.
 * You can set the maximum number of objects to be be stored in the 
 * pool from within the inspector.
 * 
 * Replace all Instantiate( myPrefab ) calls with 
 * ObjectPoolController.Instantiate( myPrefab)
 * 
 * Replace all Destroy( myObjectInstance ) calls with 
 * ObjectPoolController.Destroy( myObjectInstance )
 * 
 * Note that Awake(), and OnDestroy() get called correctly for 
 * pooled objects. However, make sure that all component data that could  
 * possibly get changed during its lifetime get reinitialized by the
 * Awake() function.
 * The Start() function gets also called, but just after the Awake() function
 * during ObjectPoolController.Instantiate(...)
 * 
 * If a poolable objects gets parented to none-poolable object, the parent must
 * be destroyed using ObjectPoolController.Destroy( ... )
 * 
 * Be aware that OnDestroy() will get called multiple times: 
 *   a) the time ObjectPoolController.Destroy() is called when the object is added
 *      to the pool
 *   b) when the object really gets destroyed (e.g. if a new scene is loaded)
 *   
 * References to pooled objects will not change to null anymore once an object has 
 * been "destroyed" and moved to the pool. Use PoolableReference if you need such checks.
 * 
 * ********************************************************************
*/


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

#pragma warning disable 1591 // undocumented XML code warning


/// <summary>
/// Add this component to your prefab to make it poolable. 
/// </summary>
/// <remarks>
/// See <see cref="ObjectPoolController"/> for an explanation how to set up a prefab for pooling.
/// </remarks>
/// <seealso cref="ObjectPoolController"/>
[AddComponentMenu( "ClockStone/PoolableObject" )]
public class PoolableObject : MonoBehaviour
{
    /// <summary>
    /// The maximum number of instances of this prefab to get stored in the pool.
    /// </summary>
    public int maxPoolSize = 10;

    /// <summary>
    /// This number of instances will be preloaded to the pool if <see cref="ObjectPoolController.Preload(GameObject)"/> is called.
    /// </summary>
    public int preloadCount = 0;

    /// <summary>
    /// If enabled the object will not get destroyed if a new scene is loaded
    /// </summary>
    public bool doNotDestroyOnLoad = false;


    /// <summary>
    /// If enabled Awake(), Start(), and OnDestroy() messages are sent to the poolable object if the object is set 
    /// active respectively inactive whenever <see cref="ObjectPoolController.Destroy(GameObject)"/> or 
    /// <see cref="ObjectPoolController.Instantiate(GameObject)"/> is called. <para/>
    /// This way it is simulated that the object really gets instantiated respectively destroyed.
    /// </summary>
    /// <remarks>
    /// The Start() function is called immedialtely after Awake() by <see cref="ObjectPoolController.Instantiate(GameObject)"/> 
    /// and not next frame. So do not set data after <see cref="ObjectPoolController.Instantiate(GameObject)"/> that Start()
    /// relies on.
    /// </remarks>
    public bool sendAwakeStartOnDestroyMessage = true;

    internal bool _isAvailableForPooling = false;
    internal bool _createdWithPoolController = false;
    internal bool _destroyMessageFromPoolController = false;
    internal bool _wasPreloaded = false;
    internal bool _wasStartCalledByUnity = false;
    internal ObjectPoolController.ObjectPool _myPool = null;
    internal int _serialNumber = 0;
    internal int _usageCount = 0;

#if UNITY_EDITOR
    protected void Awake()
    {
        //Debug.Log( string.Format( "Awake: {0} Pool:{1}", name, _myPool != null ) );
        if ( _myPool == null && !ObjectPoolController._isDuringInstantiate )
        {
            Debug.LogWarning( "Poolable object " + name + " was instantiated without ObjectPoolController" );
        }
    }
#endif

    protected void Start()
    {
        _wasStartCalledByUnity = true;
    }

    protected void OnDestroy()
    {
        if ( !_destroyMessageFromPoolController && _myPool != null )
        {
            // Poolable object was destroyed by using the default Unity Destroy() function -> Use ObjectPoolController.Destroy() instead
            // This can also happen if objects are automatically deleted by Unity e.g. due to level change or if an object is parented to an object that gets destroyed

            _myPool.Remove( this );
            //Debug.LogError( "Destroy S/N:" + _serialNumber );
        }

        _destroyMessageFromPoolController = false;
    }

    /// <summary>
    /// Gets the object's pool serial number. Each object has a unique serial number. Can be useful for debugging purposes.
    /// </summary>
    /// <returns>
    /// The serial number (starting with 1 for each pool).
    /// </returns>
    public int GetSerialNumber() // each new instance receives a unique serial number
    {
        return _serialNumber;
    }
    
    /// <summary>
    /// Gets the usage counter which gets increased each time an object is re-used from the pool.
    /// </summary>
    /// <returns>
    /// The usage counter
    /// </returns>
    public int GetUsageCount()
    {
        return _usageCount;
    }
}



/// <summary>
/// A static class used to create and destroy poolable objects.
/// </summary>
/// <remarks>
/// What is pooling? <para/>
/// GameObject.Instantiate(...) calls are relatively time expensive. If objects of the same
/// type are frequently created and destroyed it is good practice to use object pools, particularly on mobile
/// devices. This can greatly reduce the performance impact for object creation and garbage collection. <para/>
/// How does pooling work?<para/>
/// Instead of actually destroying object instances, they are just set inactive and moved to an object "pool".
/// If a new object is requested it can then simply be pulled from the pool, instead of creating a new instance. <para/>
/// Awake(), Start() and OnDestroy() are called if objects are retrieved from or moved to the pool like they 
/// were instantiated and destroyed normally.
/// </remarks>
/// <example>
/// How to set up a prefab for pooling:
/// <list type="number">
/// <item>Add the PoolableObject script component to the prefab to be pooled.
/// You can set the maximum number of objects to be be stored in the pool from within the inspector.</item>
/// <item> Replace all <c>Instantiate( myPrefab )</c> calls with <c>ObjectPoolController.Instantiate( myPrefab )</c></item>
/// <item> Replace all <c>Destroy( myObjectInstance )</c> calls with <c>ObjectPoolController.Destroy( myObjectInstance )</c></item>
/// </list>
/// Attention: Be aware that:
/// <list type="bullet">
/// <item>All data must get initialized in the Awake() or Start() function</item>
/// <item><c>OnDestroy()</c> will get called a second time once the object really gets destroyed by Unity</item>
/// <item>If a poolable objects gets parented to none-poolable object, the parent must
/// be destroyed using <c>ObjectPoolController.Destroy( ... )</c> even if it is none-poolable itself.</item>
/// </list>
/// </example>
/// <seealso cref="PoolableObject"/>
static public class ObjectPoolController
{

    static public bool isDuringPreload
    {
        get;
        private set;
    }

    // **************************************************************************************************/
    //          public functions
    // **************************************************************************************************/

    /// <summary>
    /// Retrieves an instance of the specified prefab. Either returns a new instance or it claims an instance 
    /// from the pool.
    /// </summary>
    /// <param name="prefab">The prefab to be instantiated.</param>
    /// <returns>
    /// An instance of the prefab.
    /// </returns>
    /// <remarks>
    /// Can be used on none-poolable objects as well. It is good practice to use <c>ObjectPoolController.Instantiate</c>
    /// whenever you may possibly make your prefab poolable in the future.
    /// </remarks>
    /// <seealso cref="Destroy(GameObject)"/>
    static public GameObject Instantiate( GameObject prefab )
    {
        PoolableObject prefabPool = prefab.GetComponent<PoolableObject>();
        if ( prefabPool == null )
        {
            //Debug.LogWarning( "Object " + prefab.name + " not poolable " );
            return (GameObject) GameObject.Instantiate( prefab ); // prefab not pooled, instantiate normally
        }

        GameObject go = _GetPool( prefabPool ).GetPooledInstance( null, null );

        if ( go )
        {
            return go;
        }
        else // pool is full
        {
            return InstantiateWithoutPool( prefab );
        }
    }

    /// <summary>
    /// Retrieves an instance of the specified prefab. Either returns a new instance or it claims an instance
    /// from the pool.
    /// </summary>
    /// <param name="prefab">The prefab to be instantiated.</param>
    /// <param name="position">The position in world coordinates.</param>
    /// <param name="quaternion">The rotation quaternion.</param>
    /// <returns>
    /// An instance of the prefab.
    /// </returns>
    /// <remarks>
    /// Can be used on none-poolable objects as well. It is good practice to use <c>ObjectPoolController.Instantiate</c>
    /// whenever you may possibly make your prefab poolable in the future.
    /// </remarks>
    /// <seealso cref="Destroy(GameObject)"/>
    static public GameObject Instantiate( GameObject prefab, Vector3 position, Quaternion quaternion )
    {
        PoolableObject prefabPool = prefab.GetComponent<PoolableObject>();
        if ( prefabPool == null )
        {
            // no warning displayed by design because this allows to decide later if the object will be poolable or not
            // Debug.LogWarning( "Object " + prefab.name + " not poolable "); 
            return (GameObject) GameObject.Instantiate( prefab, position, quaternion ); // prefab not pooled, instantiate normally
        }

        GameObject go = _GetPool( prefabPool ).GetPooledInstance( position, quaternion );

        if ( go )
        {
            return go;
        }
        else // pool is full
        {
            //Debug.LogWarning( "Pool Full" );
            return InstantiateWithoutPool( prefab, position, quaternion );
        }
    }

    /// <summary>
    /// Instantiates the specified prefab without using pooling.
    /// from the pool.
    /// </summary>
    /// <param name="prefab">The prefab to be instantiated.</param>
    /// <returns>
    /// An instance of the prefab.
    /// </returns>
    /// <remarks>
    /// If the prefab is poolable, the <see cref="PoolableObject"/> component will be removed.
    /// This way no warning is generated that a poolable object was created without pooling.
    /// </remarks>
    static public GameObject InstantiateWithoutPool( GameObject prefab )
    {
        return InstantiateWithoutPool( prefab, new Vector3( 0, 0, 0 ), Quaternion.identity );
    }

    /// <summary>
    /// Instantiates the specified prefab without using pooling.
    /// from the pool.
    /// </summary>
    /// <param name="prefab">The prefab to be instantiated.</param>
    /// <param name="position">The position in world coordinates.</param>
    /// <param name="quaternion">The rotation quaternion.</param>
    /// <returns>
    /// An instance of the prefab.
    /// </returns>
    /// <remarks>
    /// If the prefab is poolable, the <see cref="PoolableObject"/> component will be removed.
    /// This way no warning is generated that a poolable object was created without pooling.
    /// </remarks>
    static public GameObject InstantiateWithoutPool( GameObject prefab, Vector3 position, Quaternion quaternion )
    {
        GameObject go;
        _isDuringInstantiate = true;
        go = (GameObject) GameObject.Instantiate( prefab, position, quaternion ); // prefab not pooled, instantiate normally
        _isDuringInstantiate = false;

        PoolableObject pool = go.GetComponent<PoolableObject>();
        if ( pool )
        {
            if ( pool.doNotDestroyOnLoad )
            {
                GameObject.DontDestroyOnLoad( go );
            }

            pool._createdWithPoolController = true; // so no warning is displayed if object gets ObjectPoolCOntroller.Destroy()-ed before the component actually gets removed
#if UNITY_EDITOR            
			Component.DestroyImmediate( pool );
#else
			Component.Destroy( pool );
#endif
        }

        return go;

    }

    /// <summary>
    /// Destroys the specified game object, respectively sets the object inactive and adds it to the pool.
    /// </summary>
    /// <param name="obj">The game object.</param>
    /// <remarks>
    /// Can be used on none-poolable objects as well. It is good practice to use <c>ObjectPoolController.Destroy</c>
    /// whenever you may possibly make your prefab poolable in the future. <para/>
    /// Must also be used on none-poolable objects with poolable child objects so the poolable child objects are correctly
    /// moved to the pool.
    /// </remarks>
    /// <seealso cref="Instantiate(GameObject)"/>
    static public void Destroy( GameObject obj ) // destroys poolable and none-poolable objects. Destroys poolable children correctly
    {

        PoolableObject poolObj = obj.GetComponent<PoolableObject>();
        if ( poolObj == null )
        {
            _DetachChildrenAndDestroy( obj.transform ); // child objects may be poolable
            GameObject.Destroy( obj ); // prefab not pooled, destroy normally
            return;
        }
        if ( poolObj._myPool != null )
        {
            poolObj._myPool._SetAvailable( poolObj, true );
        }
        else
        {
            if ( !poolObj._createdWithPoolController )
            {
                Debug.LogWarning( "Poolable object " + obj.name + " not created with ObjectPoolController" );
            }
            GameObject.Destroy( obj ); // prefab not pooled, destroy normally
        }

    }

    /// <summary>
    /// Preloads as many instances to the pool so that there are at least as many as
    /// specified in <see cref="PoolableObject.preloadCount"/>. 
    /// </summary>
    /// <param name="prefab">The prefab.</param>
    /// <remarks>
    /// Use ObjectPoolController.isDuringPreload to check if an object is preloaded in the <c>Awake()</c> function.
    /// If the pool already contains at least <see cref="PoolableObject.preloadCount"/> objects, the function does nothing. 
    /// </remarks>
    /// <seealso cref="PoolableObject.preloadCount"/>
    static public void Preload( GameObject prefab ) // adds as many instances to the prefab pool as specified in the PoolableObject
    {
        PoolableObject poolObj = prefab.GetComponent<PoolableObject>();
        if ( poolObj == null )
        {
            Debug.LogWarning( "Can not preload because prefab '" + prefab.name + "' is not poolable" );
            return;
        }

        var pool = _GetPool( poolObj ); // _preloadDone is set by _GetPool

        int delta = poolObj.preloadCount - pool.GetObjectCount();
        if ( delta <= 0 )
        {
            return;
        }

        isDuringPreload = true;

        try
        {
            for ( int i = 0; i < delta; i++ )
            {
                pool.PreloadInstance();
            }
        }
        finally
        {
            isDuringPreload = false;
        }

        //Debug.Log( "preloaded: " + prefab.name + " " + poolObj.preloadCount + " times" );
    }


    // **************************************************************************************************/
    //          protected / private  functions
    // **************************************************************************************************/

    internal static int _globalSerialNumber = 0;
    internal static bool _isDuringInstantiate = false;

    internal class ObjectPool
    {
        HashSet_Flash<PoolableObject> _pool;
        PoolableObject _prefabPoolObj;

        public ObjectPool( GameObject prefab )
        {
            _prefabPoolObj = prefab.GetComponent<PoolableObject>();
        }

        private void _ValidatePooledObjectDataContainer()
        {
            if ( _pool == null ) _pool = new HashSet_Flash<PoolableObject>();
        }

        internal void Remove( PoolableObject poolObj )
        {
            _pool.Remove( poolObj );
        }

        internal int GetObjectCount()
        {
            if ( _pool == null ) return 0;
            return _pool.Count;
        }

        internal GameObject GetPooledInstance( Vector3? position, Quaternion? rotation )
        {
            _ValidatePooledObjectDataContainer();

            foreach ( PoolableObject o in _pool )
            {
                if ( o != null && o._isAvailableForPooling )
                {
                    o.transform.position = ( position != null ) ? (Vector3) position : _prefabPoolObj.transform.position;
                    o.transform.rotation = ( rotation != null ) ? (Quaternion) rotation : _prefabPoolObj.transform.rotation;
                    o.transform.localScale = _prefabPoolObj.transform.localScale;
                    o._usageCount++;
                    _SetAvailable( o, false );
                    return o.gameObject;
                }
            }

            if ( _pool.Count < _prefabPoolObj.maxPoolSize ) // add new element to pool 
            {
                return _NewPooledInstance( position, rotation ).gameObject;
            }

            // pool is full
            return null;
        }

        internal PoolableObject PreloadInstance()
        {
            _ValidatePooledObjectDataContainer();

            PoolableObject poolObj = _NewPooledInstance( null, null );

            poolObj._wasPreloaded = true;

            _SetAvailable( poolObj, true );

            return poolObj;
        }

        private PoolableObject _NewPooledInstance( Vector3? position, Quaternion? rotation )
        {
            GameObject go;

            _isDuringInstantiate = true;

            if ( position != null && rotation != null )
            {
                go = (GameObject) GameObject.Instantiate( _prefabPoolObj.gameObject, (Vector3) position, (Quaternion) rotation );
            }
            else
            {
                go = (GameObject) GameObject.Instantiate( _prefabPoolObj.gameObject );
            }

            _isDuringInstantiate = false;

            PoolableObject poolObj = go.GetComponent<PoolableObject>();
            poolObj._createdWithPoolController = true;
            poolObj._myPool = this;
            poolObj._isAvailableForPooling = false;
            poolObj._serialNumber = ++_globalSerialNumber;
            poolObj._usageCount++;
            
            if ( poolObj.doNotDestroyOnLoad )
            {
                GameObject.DontDestroyOnLoad( go );
            }

            _pool.Add( poolObj );
            return poolObj;

        }

        internal void _SetAvailable( PoolableObject poolObj, bool b )
        {
            poolObj._isAvailableForPooling = b;

            if ( b )
            {
                if ( poolObj.sendAwakeStartOnDestroyMessage )
                {
                    poolObj._destroyMessageFromPoolController = true;
                }

                poolObj.gameObject.transform.parent = null; // object could still be parented, so detach

                _RecursivelySetInactiveAndSendMessages( poolObj.gameObject, poolObj );

                poolObj.gameObject.name = "pooled:" + poolObj._myPool._prefabPoolObj.name;
             
            }
            else
            {
                _SetActiveAndSendMessages( poolObj.gameObject, poolObj );

                poolObj.gameObject.name = poolObj._myPool._prefabPoolObj.name;
            }
        }

        private void _SetActiveAndSendMessages( GameObject obj, PoolableObject parentPoolObj )
        {
			obj.SetActive(true);

            if ( parentPoolObj.sendAwakeStartOnDestroyMessage )
            {
                obj.BroadcastMessage( "Awake", null, SendMessageOptions.DontRequireReceiver );

                if ( obj.activeInHierarchy && // Awake could deactivate object
                        parentPoolObj._wasStartCalledByUnity ) // for preloaded objects Unity will call Start
                {
                    obj.BroadcastMessage( "Start", null, SendMessageOptions.DontRequireReceiver );
                }
            }
        }

        private void _RecursivelySetInactiveAndSendMessages( GameObject obj, PoolableObject parentPoolObj )
        {
            //now recursively do the same for all children
            for ( int i = 0; i < obj.transform.childCount; i++ )
            {
                Transform child = obj.transform.GetChild( i );

                var poolableChild = child.gameObject.GetComponent<PoolableObject>();

                if ( poolableChild && poolableChild._myPool != null ) //if child is poolable itself it has to be detached and moved to the pool
                {
                    _SetAvailable( poolableChild, true );
                }
                else
                {
                    _RecursivelySetInactiveAndSendMessages( child.gameObject, parentPoolObj );
                }
            }

            if ( parentPoolObj.sendAwakeStartOnDestroyMessage )
            {
                obj.SendMessage( "OnDestroy", null, SendMessageOptions.DontRequireReceiver );
            }

            obj.SetActive(false);
        }
    }

    static private Dictionary<GameObject, ObjectPool> _pools = new Dictionary<GameObject, ObjectPool>();

    static internal ObjectPool _GetPool( PoolableObject prefabPoolComponent )
    {
        ObjectPool pool;

        GameObject prefab = prefabPoolComponent.gameObject;

        if ( !_pools.TryGetValue( prefab, out pool ) )
        {
            pool = new ObjectPool( prefab );
            _pools.Add( prefab, pool );
        }

        return pool;
    }

    static private void _DetachChildrenAndDestroy( Transform transform )
    {
        int childCount = transform.childCount;

        Transform[ ] children = new Transform[ childCount ];

        int i;
        for ( i = 0; i < childCount; i++ )
        {
            children[ i ] = transform.GetChild( i );
        }
        transform.DetachChildren();

        for ( i = 0; i < childCount; i++ )
        {
            GameObject obj = children[ i ].gameObject;
            if ( obj )
            {
                ObjectPoolController.Destroy( obj );
            }
        }

    }
}

/// <summary>
/// Auxiliary class to overcome the problem of references to pooled objects that should become <c>null</c> when 
/// objects are moved back to the pool after calling <see cref="ObjectPoolController.Destroy(GameObject)"/>.
/// </summary>
/// <typeparam name="T">A <c>UnityEngine.Component</c></typeparam>
/// <example>
/// Instead of a normal reference to a script component on a poolable object use 
/// <code>
/// MyScriptComponent scriptComponent = PoolableObjectController.Instantiate( prefab ).GetComponent&lt;MyScriptComponent&gt;();
/// var myReference = new PoolableReference&lt;MyScriptComponent&gt;( scriptComponent );
/// if( myReference.Get() != null ) // will check if poolable instance still belongs to the original object
/// {
///     myReference.Get().MyComponentFunction();
/// }
/// </code>
/// </example>
public class PoolableReference<T>  where T : Component
{
    PoolableObject _pooledObj;
    int _initialUsageCount;

#if UNITY_FLASH
    Component _objComponent;
#else 
    T _objComponent;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolableReference&lt;T&gt;"/> class with a <c>null</c> reference.
    /// </summary>
    public PoolableReference( )
    {
        Reset();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolableReference&lt;T&gt;"/> class with the specified reference.
    /// </summary>
    /// <param name="componentOfPoolableObject">The referenced component of the poolable object.</param>
#if UNITY_FLASH
    public PoolableReference( Component componentOfPoolableObject )
#else
    public PoolableReference( T componentOfPoolableObject )
#endif
    {
        Set( componentOfPoolableObject );
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolableReference&lt;T&gt;"/> class from 
    /// a given <see cref="PoolableReference&lt;T&gt;"/>.
    /// </summary>
    /// <param name="poolableReference">The poolable reference.</param>
    public PoolableReference( PoolableReference<T> poolableReference )
    {
        _objComponent = poolableReference._objComponent;
        _pooledObj = poolableReference._pooledObj;
        _initialUsageCount = poolableReference._initialUsageCount;
    }

    /// <summary>
    /// Resets the reference to <c>null</c>.
    /// </summary>
    public void Reset()
    {
        _pooledObj = null;
        _objComponent = null;
        _initialUsageCount = 0;
    }

    /// <summary>
    /// Gets the reference to the script component, or <c>null</c> if the object was 
    /// already destroyed or moved to the pool.
    /// </summary>
    /// <returns>
    /// The reference to <c>T</c> or null
    /// </returns>
    public T Get()
    {
        if ( !_objComponent ) return null;

		if (_pooledObj == null) return null;
        if ( _pooledObj._usageCount != _initialUsageCount || _pooledObj._isAvailableForPooling )
        {
            _objComponent = null;
            _pooledObj = null;
            return null;
        }
        return (T)_objComponent;
    }

    /// <summary>
    /// Sets the reference to a poolable object with the specified component.
    /// </summary>
    /// <param name="componentOfPoolableObject">The component of the poolable object.</param>
#if UNITY_FLASH
    public void Set( Component componentOfPoolableObject )
#else
    public void Set( T componentOfPoolableObject )
#endif
    {
        if ( !componentOfPoolableObject )
        {
            Reset();
            return;
        }
        _objComponent = (T)componentOfPoolableObject;
        _pooledObj = _objComponent.GetComponent<PoolableObject>();
        if ( !_pooledObj )
        {
            Debug.LogError( String.Format("Object for PoolableReference must be poolable ({0})", typeof(T).Name) );
            return;
        }
        _initialUsageCount = _pooledObj._usageCount;
    }
}