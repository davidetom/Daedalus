using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public bool isMoving;
    private Vector2 input;

    [Header("Tilemap Reference")]
    public Tilemap tilemap;
    public TileBase muraTile;

    [Header("Door Interaction")]
    public float interactRange = 1f; // distanza massima per interagire con la porta
    public KeyCode interactKey = KeyCode.E;
    public bool hasKey = false; //all'inizio non ha la chiave


    public int coinCount;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleMovement();

        // Controlla input per aprire la porta
        if (Input.GetKeyDown(interactKey))
        {
            TryOpenNearbyDoor();
        }
    }

    void HandleMovement()
    {
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);
                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;

                if (IsWalkable(targetPos))
                    StartCoroutine(Move(targetPos));
            }
        }

        animator.SetBool("isMoving", isMoving);
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        isMoving = false;
    }

    public bool IsWalkable(Vector3 targetPos)
    {
        if (tilemap == null)
        {
            Debug.LogWarning("Tilemap non assegnata!");
            return true;
        }

        Vector3Int cellPosition = tilemap.WorldToCell(targetPos);
        TileBase tileAtPosition = tilemap.GetTile(cellPosition);

        bool isWall = (muraTile != null && tileAtPosition == muraTile);

        if (!isWall && tileAtPosition != null)
        {
            var colliderType = tilemap.GetColliderType(cellPosition);
            isWall = (colliderType != Tile.ColliderType.None);
        }

        // 🔹 NUOVO CONTROLLO: verifica se c'è una porta chiusa in quella cella
    Collider2D doorCollider = Physics2D.OverlapPoint(targetPos);
    if (doorCollider != null)
    {
        DoorController door = doorCollider.GetComponent<DoorController>();
        if (door != null && !door.IsOpen())
        {
            return false; // Porta chiusa → non camminabile
        }
    }

        return !isWall;
    }

    // ----------------- NUOVA PARTE -----------------
    void TryOpenNearbyDoor()
{
    DoorController[] doors = GameObject.FindObjectsByType<DoorController>(FindObjectsSortMode.None);

    foreach (var door in doors)
    {
        float distance = Vector3.Distance(transform.position, door.transform.position);
        if (distance <= interactRange)
        {
            door.TryOpen(this); // <--- passo il Player stesso
            break;
        }
    }
}

}