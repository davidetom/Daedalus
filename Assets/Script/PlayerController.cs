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

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
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

    // METODO 2: Detection diretta sulla Tilemap (più preciso per tile specifici)
    public bool IsWalkable(Vector3 targetPos)
    {
        if (tilemap == null)
        {
            Debug.LogWarning("Tilemap non assegnata!");
            return true; // Fallback: assume walkable
        }
        
        // Converti posizione world a coordinate tilemap
        Vector3Int cellPosition = tilemap.WorldToCell(targetPos);
        
        // Ottieni il tile nella posizione
        TileBase tileAtPosition = tilemap.GetTile(cellPosition);
        
        // Controlla se è un tile muro
        bool isWall = (muraTile != null && tileAtPosition == muraTile);
        
        // Metodo alternativo: controlla se il tile ha un collider
        if (!isWall && tileAtPosition != null)
        {
            // Verifica il tipo di collider del tile
            var colliderType = tilemap.GetColliderType(cellPosition);
            isWall = (colliderType != Tile.ColliderType.None);
        }
        
        bool isWalkable = !isWall;
        
        return isWalkable;
    }
}
