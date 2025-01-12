1. Для управления мышью задействую интерфейсы  IPointerDownHandler, IDragHandler, IPointerUpHandler и реализую их методы, а также задам несколько параметров.

```csharp
public class DragandDrop : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Vector3 _offset; 
    private Rigidbody2D _rigidbody2D;
    private bool _isDragging = false; 			//тащу объект или нет
    private RectTransform objectRectTransform;	
    private Vector3 baseScale;					//базовый масштаб вещи

    private BoxCollider2D bgCollider;
    private RectTransform bgRectTransform;

    public float scrollSpeed = 100f;			//скорость прокручивания мышью
    public float inertiaFactor = 0.95f;
    private float scrollVelocity = 0f;
	
	...
```
2. Стартую, не пробуждаюсь, и задаю значения параметрам
```csharp
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
```
3. Каждый кадр делаю:
```csharp
void Update()
    {
        HandleBackgroundScroll();																//обрабатываю скролл (хотя лучше это вынести в отдельный скрипт, в будущем)

        ApplyInertia();																			//добавляю инерцию

        if (_rigidbody2D.bodyType == RigidbodyType2D.Dynamic)									//если двигаю объект, то проверяю границы
        {
            Vector3 position = transform.position;
            BoundsCheck(position);
        }

        if (Input.touchCount > 0)																//пробовал по старому реализовать управление сенсорным экраном. Не хорошо. Нужно будет перевести на InputSystem
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
```	
4. Как тоолько нажал на объект (Событие такое)
```csharp
	public void OnPointerDown(PointerEventData eventData)				
    {
        _isDragging = true;												//тащу и это верно
        objectRectTransform.localScale = baseScale;						//возвращаю начальный мышью
        _offset = transform.position - (Vector3) eventData.position;
        SetKinematic();													//объект не поддается гравитации
    }
```	
5. Тащу объект (Событие такое)
```csharp
	public void OnDrag(PointerEventData eventData) 														
    {
        if (_isDragging)
        {
            Vector3 position = new Vector3(eventData.position.x, eventData.position.y, 0) + _offset;	//меняю положение

            BoundsCheck(position);																		//проверяю границы
            SetKinematic();																				//объект не поддается гравитации
        }
    }
```	
6. Отпустил объект (Последнее событие)
```csharp
	public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;		//нет не тащу и это верно)
        SetDynamic();				//гравитация начала действовать
        CheckIfOnShelf();			//проверяю есть ли полка рядом
    }
```	
7. Включаю гравитацию
```csharp
	private void SetDynamic()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody2D.gravityScale = 1;
        _rigidbody2D.AddForce(new Vector2(0, -50), ForceMode2D.Impulse);	//немного толкаю объект, чтобы он быстрее падал, красиво
    }
```
8.  Отключаю гравитацию, сбрасываю все ускорения
```csharp
	private void SetKinematic()
    {
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody2D.gravityScale = 0;
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0;
    }
```
9. Проверяю не выходит ли объект за границы подложки - Background
```csharp
	private void BoundsCheck(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, bgCollider.bounds.min.x + (objectRectTransform.rect.width / 1.5f), bgCollider.bounds.max.x - (objectRectTransform.rect.width / 1.5f));
        position.y = Mathf.Clamp(position.y, bgCollider.bounds.min.y + (objectRectTransform.rect.height / 1.5f), bgCollider.bounds.max.y - (objectRectTransform.rect.height / 1.5f));

        transform.position = position;
    }
```
10. Проверяю находится ли объект над полкой (слой Shelf, тег Shelf)
```csharp
	private void CheckIfOnShelf()
    {
        int layerMask = LayerMask.GetMask("Shelf"); 													//буду "стрелять" только по полкам
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1, layerMask);
        if (hit.collider != null && hit.collider.CompareTag("Shelf"))
        {
            Vector3 storagePoint = hit.collider.transform.position;
            Transform child = hit.collider.gameObject.transform.Find("Storage");						//нахожу дочерний объект полки
            if (child != null)
            {
                storagePoint = hit.collider.gameObject.transform.TransformPoint(child.localPosition);	//беру координаты дочернего объекта полки
            }
            objectRectTransform.localScale = baseScale * 0.5f;											//меняю размер объекта-яблока
            transform.position = new Vector3(storagePoint.x, storagePoint.y, storagePoint.z);			//устанавливаю объект та место дочернего объекта полки, так можно расставлять в дальнейшем несколько разных объектов

            SetKinematic();																				//объект не поддается гравитации
        }
    }
```
11. Скролю задний план (подложка Background) с помощью мыши. Для сенсора не реализовал. А мог бы... )
```csharp
	private void HandleBackgroundScroll()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            scrollVelocity = scrollInput * scrollSpeed;
        }

        bgRectTransform.position = new Vector3(bgRectTransform.position.x + scrollVelocity, bgRectTransform.position.y, bgRectTransform.position.z); 

 
        float minX = -(bgRectTransform.rect.width / 4);		// ограничение - насколько можно скроллить влево и вправо. Не идеальная реализация, нужно обдумать. Сейчас есть небольшой зазор по краям.
        float maxX = (bgRectTransform.rect.width / 2);


        bgRectTransform.position = new Vector3(Mathf.Clamp(bgRectTransform.position.x, minX, maxX), bgRectTransform.position.y, bgRectTransform.position.z);
    }
```	
12. Инерция. Добавил инерцию к скроллу, так будет повеселей.
```csharp

	...
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
```
