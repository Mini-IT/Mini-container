using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniContainer
{
    [DefaultExecutionOrder(-7000)]
    public class CompositionRoot : MonoBehaviour
    {
#pragma warning disable 0649 // is never assigned to, and will always have its default value null.
        public static CompositionRoot Instance { get; private set; }
        private CompositionRoot[] _objects;
 
        [SerializeField]
        private List<RootContainer> _rootContainers;

        private IContainer _container;
        private IDIService _diService;

#pragma warning restore 0649

        private void Awake()
        {
            _objects = FindObjectsOfType<CompositionRoot>();
            if (Instance == null)
            {
                Instance = this;
            }

            if (_objects.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            InitSystem();
        }

        public void SubContainerInit(SubContainer subContainer)
        {
            subContainer.Init(_diService, _container);
        }
        
        private void Update()
        {
            _container?.RunUpdate();
        }

        private void InitSystem()
        {
            _diService = new DIService();
            _container = _diService.GenerateContainer();
            if (_rootContainers.Count == 0)
            {
                Logs.InvalidOperation("Root container should not be null! Check CompositionRoot in the inspector!");
            }
            for (var i = 0; i < _rootContainers.Count; i++)
            {
                var rootContainer = _rootContainers[i];
                if (rootContainer == null)
                {
                    Logs.InvalidOperation("Root container should not be null! Check CompositionRoot in the inspector!");
                }
                rootContainer.Init(_diService, _container);
            }
            for (var i = 0; i < _rootContainers.Count; i++)
            {
                _rootContainers[i].ResolveContainer();
            }
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnApplicationPause(bool pause)
        {
            _container?.RunApplicationPause(pause);
        }

        private void OnApplicationFocus(bool focus)
        {
            _container?.RunApplicationFocus(focus);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _container?.RunSceneLoaded(scene.buildIndex);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _container?.RunSceneUnloaded(scene.buildIndex);
        }
        
        private void OnDestroy()
        {
            if (_objects.Length == 1)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                _container.ReleaseAll();
            }
        }
    }
}
