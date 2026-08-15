using System;
using System.Collections.Generic;
using Block;
using Level;
using Shooter;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public class GameplayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelData _levelData;
        [SerializeField] private LevelGenerator _levelGenerator;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private ShooterQueue _shooterQueue;

        [SerializeField] private GameObject _projectilePrefab;

        [Header("Slot Yapilandirmasi")]
        [Tooltip("Slotlarin yerlesecegi dunya pozisyonu merkezi.")]
        [SerializeField] private Vector3 _slotsOrigin = new Vector3(0f, -0.90f, 0f);

        [Tooltip("Slotlar arasi bosluk.")]
        [SerializeField] private float _slotPadding = 0.08f;

        [Tooltip("Slot boyutu.")]
        [SerializeField] private float _slotSize = 0.25f;

        [Header("Slot Gorsel Ayarlari")]
        [SerializeField] private GameObject _slotVisualPrefab;

        [Tooltip("Otomatik slot cercevesi rengi.")]
        [SerializeField] private Color _slotFrameColor = new Color(0.06f, 0.04f, 0.16f, 0.75f);

        private readonly List<ShooterSlot> _slots = new List<ShooterSlot>();
        private Camera _mainCam;
        private Material _slotMaterial;
        private bool _isGameOver = false;

        public event Action OnLevelWon;

        public event Action OnLevelFailed;

        private void Start()
        {
            _mainCam = Camera.main;
            InitializeLevel();
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDestroy()
        {
            if (_gridManager != null)
            {
                _gridManager.OnLevelComplete   -= HandleLevelWon;
                _gridManager.OnFrontRowChanged -= CheckFailCondition;
            }

            if (_shooterQueue != null)
            {
                _shooterQueue.OnBlockSelected -= HandleBlockSelectedFromQueue;
            }

            foreach (var slot in _slots)
            {
                if (slot != null)
                    slot.OnSlotFreed -= HandleSlotFreed;
            }

            if (_slotMaterial != null)
                Destroy(_slotMaterial);
        }

        // ── Input & Tiklama Algilama ───────────────────────────────

        private void HandleInput()
        {
            if (_isGameOver) return;

            bool pointerDown = false;
            Vector3 pointerPos = Vector3.zero;

            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerPos = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            #endif

            if (!pointerDown && Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                pointerPos = Input.mousePosition;
            }

            if (pointerDown && _mainCam != null)
            {
                Ray ray = _mainCam.ScreenPointToRay(pointerPos);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    var shooterBlock = hit.collider.GetComponentInParent<ShooterBlock>();
                    if (shooterBlock != null && !shooterBlock.IsInSlot)
                    {
                        if (_shooterQueue != null && _shooterQueue.IsFrontBlock(shooterBlock))
                        {
                            shooterBlock.HandleClick();
                        }
                    }
                }
            }
        }

        public void InitializeLevel()
        {
            _isGameOver = false;

            if (_levelGenerator == null)
            {
                _levelGenerator = GetComponent<LevelGenerator>();
                if (_levelGenerator == null)
                    _levelGenerator = FindAnyObjectByType<LevelGenerator>();
            }

            if (_levelData == null && _levelGenerator != null)
            {
                _levelData = _levelGenerator.CurrentLevelData;
            }

            if (_levelData == null)
            {
                Debug.LogError("[GameplayController] LevelData bulunamadi!", this);
                return;
            }

            if (_gridManager == null)
            {
                _gridManager = GetComponent<GridManager>();
                if (_gridManager == null)
                    _gridManager = gameObject.AddComponent<GridManager>();
            }

            if (_levelGenerator != null)
            {
                var columns = _levelGenerator.GenerateGrid(_levelData);
                _gridManager.RegisterColumns(columns);
            }

            _gridManager.OnLevelComplete   += HandleLevelWon;
            _gridManager.OnFrontRowChanged += CheckFailCondition;

            CreateSlots(_levelData.SlotCount);

            if (_shooterQueue == null)
            {
                _shooterQueue = GetComponentInChildren<ShooterQueue>();
                if (_shooterQueue == null)
                {
                    var queueObj = new GameObject("ShooterQueue");
                    queueObj.transform.SetParent(transform);
                    _shooterQueue = queueObj.AddComponent<ShooterQueue>();
                }
            }

            _shooterQueue.OnBlockSelected += HandleBlockSelectedFromQueue;
            _shooterQueue.InitializeQueue(
                _levelData,
                _levelGenerator.BlockPrefab,
                _levelGenerator.ApplyBlockColor
            );

            Debug.Log("[GameplayController] Level basariyla baslatildi.");
        }

        private void CreateSlots(int count)
        {
            foreach (var s in _slots)
            {
                if (s != null) Destroy(s.gameObject);
            }
            _slots.Clear();

            EnsureSlotMaterial();

            float totalWidth = count * _slotSize + (count - 1) * _slotPadding;
            float startX = _slotsOrigin.x - (totalWidth / 2f) + (_slotSize / 2f);

            for (int i = 0; i < count; i++)
            {
                var slotObj = new GameObject($"ShooterSlot_{i}");
                slotObj.transform.SetParent(transform);

                float x = startX + i * (_slotSize + _slotPadding);
                slotObj.transform.position = new Vector3(x, _slotsOrigin.y, _slotsOrigin.z);

                CreateSlotVisual(slotObj);

                var slot = slotObj.AddComponent<ShooterSlot>();
                slot.OnSlotFreed += HandleSlotFreed;
                _slots.Add(slot);
            }
        }

        private void CreateSlotVisual(GameObject slotObj)
        {
            if (_slotVisualPrefab != null)
            {
                var visual = Instantiate(_slotVisualPrefab, slotObj.transform);
                visual.transform.localPosition = new Vector3(0f, 0f, 0.02f);
                return;
            }

            // Fallback: Saydam koyu kare taban
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "SlotFrame";
            quad.transform.SetParent(slotObj.transform);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            quad.transform.localScale = new Vector3(_slotSize * 0.95f, _slotSize * 0.95f, 1f);

            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = quad.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = _slotMaterial;
            }
        }

        private void EnsureSlotMaterial()
        {
            if (_slotMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") 
                             ?? Shader.Find("Sprites/Default") 
                             ?? Shader.Find("Unlit/Color");

                _slotMaterial = new Material(shader);
                _slotMaterial.color = _slotFrameColor;
            }
        }

        private ShooterSlot FindEmptySlot()
        {
            foreach (var slot in _slots)
            {
                if (!slot.IsOccupied)
                    return slot;
            }
            return null;
        }

        private void HandleBlockSelectedFromQueue(ShooterBlock block)
        {
            if (_isGameOver || block == null || block.IsInSlot) return;

            var emptySlot = FindEmptySlot();
            if (emptySlot == null)
            {
                Debug.Log("[GameplayController] Tum slotlar dolu! Blok yerlestirilemiyor.");
                CheckFailCondition();
                return;
            }

            _shooterQueue.RemoveFromQueue(block);

            emptySlot.PlaceAndFire(
                block,
                _gridManager,
                _projectilePrefab,
                _levelGenerator != null ? _levelGenerator.ApplyBlockColor : null
            );

            CheckFailCondition();
        }

        private void HandleSlotFreed(ShooterSlot slot)
        {
            if (_isGameOver) return;
            CheckFailCondition();
        }

        private void HandleLevelWon()
        {
            if (_isGameOver) return;
            _isGameOver = true;

            Debug.Log("=========================================");
            Debug.Log("🎉 [GameplayController] TEBRIKLER! LEVEL TAMAMLANDI! 🎉");
            Debug.Log("=========================================");

            OnLevelWon?.Invoke();
        }

        private void CheckFailCondition()
        {
            if (_isGameOver) return;

            if (_gridManager != null && _gridManager.IsAllEmpty())
                return;

            bool allOccupied = true;
            foreach (var slot in _slots)
            {
                if (!slot.IsOccupied)
                {
                    allOccupied = false;
                    break;
                }
            }

            if (!allOccupied) return;

            bool anyCanFire = false;
            foreach (var slot in _slots)
            {
                if (slot.IsOccupied && _gridManager.HasMatchInFrontRow(slot.OccupiedBy.Type))
                {
                    anyCanFire = true;
                    break;
                }
            }

            if (!anyCanFire)
            {
                _isGameOver = true;
                Debug.LogWarning("=========================================");
                Debug.LogWarning("💀 [GameplayController] LEVEL FAIL! Tum slotlar dolu ve on sirada eslesen renk yok! 💀");
                Debug.LogWarning("=========================================");

                OnLevelFailed?.Invoke();
            }
        }
    }
}
