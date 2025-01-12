using UnityEngine;
using UnityEngine.EventSystems;

public class DragandDrop : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Vector3 _offset; 
    private Rigidbody2D _rigidbody2D;
    private bool _isDragging = false;
    private RectTransform objectRectTransform;
    private Vector3 baseScale;

    private BoxCollider2D bgCollider;
    private RectTransform bgRectTransform;

    public float scrollSpeed = 100f;
    public float inertiaFactor = 0.95f;
    private float scrollVelocity = 0f;

    private Vector2 lastTouchPosition;

    void Start()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        objectRectTransform = gameObject.GetComponent<RectTransform>();
        baseScale = objectRectTransform.localScale;

        GameObject bg = GameObject.Find("Background");
        bgCollider = bg.GetComponent<BoxCollider2D>();
        bgRectTransform = bg.GetComponent<RectTransform>();        
        if (bgCollider == null)
        {
            bgCollider = bg.AddComponent<BoxCollider2D>();
        }
        else
        {
            bgCollider = bg.GetComponent<BoxCollider2D>();
        }
        bgCollider.size = new Vector2(bgRectTransform.rect.width, bgRectTransform.rect.height);

        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        HandleBackgroundScroll();

        ApplyInertia();

        if (_rigidbody2D.bodyType == RigidbodyType2D.Dynamic)
        {
            Vector3 position = transform.position;
            BoundsCheck(position);
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = touch.position;

            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = touchPosition;

            if (_isDragging)
            {
                Vector3 position = new Vector3(touchPosition.x, touchPosition.y, 0) + _offset;
                BoundsCheck(position);
                SetKinematic();
            }

            if (touch.phase == TouchPhase.Began)
            {
                OnPointerDown(pointerEventData);
            }
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                OnPointerUp(pointerEventData);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        objectRectTransform.localScale = baseScale;
        _offset = transform.position - (Vector3) eventData.position;
        SetKinematic();
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            Vector3 position = new Vector3(eventData.position.x, eventData.position.y, 0) + _offset;

            BoundsCheck(position);
            SetKinematic();
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
        SetDynamic();
        CheckIfOnShelf();
    }

    private void SetDynamic()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody2D.gravityScale = 1;
        _rigidbody2D.AddForce(new Vector2(0, -50), ForceMode2D.Impulse);
    }
    private void SetKinematic()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody2D.gravityScale = 0;
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0;
    }

    private void BoundsCheck(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, bgCollider.bounds.min.x + (objectRectTransform.rect.width / 1.5f), bgCollider.bounds.max.x - (objectRectTransform.rect.width / 1.5f));
        position.y = Mathf.Clamp(position.y, bgCollider.bounds.min.y + (objectRectTransform.rect.height / 1.5f), bgCollider.bounds.max.y - (objectRectTransform.rect.height / 1.5f));

        transform.position = position;
    }

    private void CheckIfOnShelf()
    {
        int layerMask = LayerMask.GetMask("Shelf");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1, layerMask);
        if (hit.collider != null && hit.collider.CompareTag("Shelf"))
        {
            Vector3 storagePoint = hit.collider.transform.position;
            Transform child = hit.collider.gameObject.transform.Find("Storage");
            if (child != null)
            {
                storagePoint = hit.collider.gameObject.transform.TransformPoint(child.localPosition);
            }
            objectRectTransform.localScale = baseScale * 0.5f;
            transform.position = new Vector3(storagePoint.x, storagePoint.y, storagePoint.z);

            SetKinematic();
        }
    }

    private void HandleBackgroundScroll()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            scrollVelocity = scrollInput * scrollSpeed;
        }

        bgRectTransform.position = new Vector3(bgRectTransform.position.x + scrollVelocity, bgRectTransform.position.y, bgRectTransform.position.z);

 
        float minX = -(bgRectTransform.rect.width / 4);
        float maxX = (bgRectTransform.rect.width / 2);


        bgRectTransform.position = new Vector3(Mathf.Clamp(bgRectTransform.position.x, minX, maxX), bgRectTransform.position.y, bgRectTransform.position.z);
    }

    private void ApplyInertia()
    {
        if (Mathf.Abs(scrollVelocity) > 0.01f)
        {
            scrollVelocity *= inertiaFactor; 
        }
        else
        {
            scrollVelocity = 0f;
        }
    }
}
