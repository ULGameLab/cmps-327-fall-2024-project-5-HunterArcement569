using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// FSM States for the enemy
public enum EnemyState { STATIC, CHASE, REST, MOVING, DEFAULT };

public enum EnemyFollowing { RANDOM , CHASING };

public enum EnemyBehavior {EnemyBehavior1, EnemyBehavior2, EnemyBehavior3 };

public class Enemy : MonoBehaviour
{
    //pathfinding
    protected PathFinder pathFinder;
    public GenerateMap mapGenerator;
    protected Queue<Tile> path;
    protected GameObject playerGameObject;

    public Tile currentTile;
    protected Tile targetTile;
    public Vector3 velocity;

    //properties
    public float speed = 1.0f;
    public float visionDistance = 5;
    public int maxCounter = 5;
    protected int playerCloseCounter;

    protected EnemyState state = EnemyState.DEFAULT;
    public EnemyFollowing following = EnemyFollowing.RANDOM;
    protected Material material;

    public EnemyBehavior behavior = EnemyBehavior.EnemyBehavior1;

    public bool seesPlayer = false;

    private Vector3 castpos;
    public float distFromPlayer;

    private void FixedUpdate()
    {
        castpos = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        distFromPlayer = Vector3.Distance(castpos, playerGameObject.GetComponent<Player>().rayCastDirection);
    }

    // Start is called before the first frame update
    void Start()
    {
        path = new Queue<Tile>();
        pathFinder = new PathFinder();
        playerGameObject = GameObject.FindWithTag("Player");
        playerCloseCounter = maxCounter;
        material = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (mapGenerator.state == MapState.DESTROYED) return;

        // Stop Moving the enemy if the player has reached the goal
        if (playerGameObject.GetComponent<Player>().IsGoalReached() || playerGameObject.GetComponent<Player>().IsPlayerDead())
        {
            //Debug.Log("Enemy stopped since the player has reached the goal or the player is dead");
            return;
        }

        //figure out if the enemy can see the player
        Ray ray = new Ray(castpos, (playerGameObject.GetComponent<Player>().rayCastDirection - castpos).normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, visionDistance, ~gameObject.layer)) //raycast hit something, not including the enemy layer
        {
            if (hit.collider.gameObject.layer == playerGameObject.layer)
            {
                seesPlayer = true; //we hit the player first
            }
            else
            {
                seesPlayer = false; //we hit a wall first
            }
        }
        else
        {
            seesPlayer = false;
        }
        //force check for the distance just in case there is any funny business with the raycast
        if(Vector3.Distance(this.gameObject.transform.position, playerGameObject.transform.position) > visionDistance) seesPlayer = false;

        switch (behavior)
        {
            case EnemyBehavior.EnemyBehavior1:
                HandleEnemyBehavior1();
                break;
            case EnemyBehavior.EnemyBehavior2:
                HandleEnemyBehavior2();
                break;
            case EnemyBehavior.EnemyBehavior3:
                HandleEnemyBehavior3();
                break;
            default:
                break;
        }

    }

    public void Reset()
    {
        path.Clear();
        state = EnemyState.DEFAULT;
        currentTile = FindWalkableTile();
        transform.position = currentTile.transform.position;
    }

    Tile FindWalkableTile()
    {
        Tile newTarget = null;
        int randomIndex = 0;
        while (newTarget == null || !newTarget.mapTile.Walkable)
        {
            randomIndex = (int)(Random.value * mapGenerator.width * mapGenerator.height - 1);
            newTarget = GameObject.Find("MapGenerator").transform.GetChild(randomIndex).GetComponent<Tile>();
        }
        return newTarget;
    }

    // Dumb Enemy: Keeps Walking in Random direction, Will not chase player
    private void HandleEnemyBehavior1()
    {
        switch (state)
        {
            case EnemyState.DEFAULT: // generate random path 
                
                //Changed the color to white to differentiate from other enemies --> default color
                material.color = Color.white;
                
                if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;
                
                //if target reached
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;
            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // TODO: Enemy chases the player when it is nearby
    private void HandleEnemyBehavior2()
    {    
        //if we can see the player and are still moving randomly, end that behavior so that the next block can swap over to a new target
        if(following == EnemyFollowing.RANDOM && seesPlayer)
        {
            path.Clear();
            //Changed the color to red to differentiate from other enemies
            material.color = Color.red;

            if (path.Count <= 0) path = pathFinder.FindPathAStar(currentTile, playerGameObject.GetComponent<Player>().currentTile);

            if (path.Count > 0)
            {
                targetTile = path.Dequeue();
                state = EnemyState.MOVING;
            }
            following = EnemyFollowing.CHASING;
        }

        switch (state)
        {
            case EnemyState.DEFAULT:

                if (seesPlayer)
                {
                    //Changed the color to red to differentiate from other enemies
                    material.color = Color.red;

                    if (path.Count <= 0) path = pathFinder.FindPathAStar(currentTile, playerGameObject.GetComponent<Player>().currentTile);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                    following = EnemyFollowing.CHASING;
                }
                else
                {
                    //Changed the color to gray to differentiate from other enemies --> default color
                    material.color = Color.gray;

                    if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                    following = EnemyFollowing.RANDOM;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                //if target reached
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;
            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // TODO: Targets tile nearby to the player if player is spotted, using same LOS check as for Behavior2
    //       randomly moves otherwise
    private void HandleEnemyBehavior3()
    {
        //calculate a nearby tile to the player (start by getting all passable tiles that are of indexes near to the player's currentTile field)
        //then randomly select one to be the target and designate it as such, then use the second parameter of the FindPathAStar to find it in both cases below
        List<Tile> tiles = new List<Tile>();
        for (int x = -1 * maxCounter; x <= maxCounter; x++)
        {
            for (int y = -1 * maxCounter; y <= maxCounter; y++)
            {
                try
                {
                    if (GenerateMap.singleton.tileList[x, y].isPassable) //only add passable tiles so this enemy cannot enter walls
                    {
                        tiles.Add(GenerateMap.singleton.tileList[x, y]);
                    }
                }
                catch { } //try catch so we don't have to manually check on if the index is inbounds
            }
        }
        Tile target = tiles[Random.Range(0, tiles.Count)];

        //if we can see the player and are still moving randomly, end that behavior so that the next block can swap over to a new target
        if (following == EnemyFollowing.RANDOM && seesPlayer)
        {
            path.Clear();
            //Changed the color to red to differentiate from other enemies
            material.color = Color.red;

            if (path.Count <= 0) path = pathFinder.FindPathAStar(currentTile, target); //change the second paremeter to get the needed result!

            if (path.Count > 0)
            {
                targetTile = path.Dequeue();
                state = EnemyState.MOVING;
            }
            following = EnemyFollowing.CHASING;
        }

        switch (state)
        {
            case EnemyState.DEFAULT:

                if (seesPlayer)
                {
                    //Changed the color to red to differentiate from other enemies
                    material.color = Color.red;

                    if (path.Count <= 0) path = pathFinder.FindPathAStar(currentTile, target);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                    following = EnemyFollowing.CHASING;
                }
                else
                {
                    //Changed the color to blue to differentiate from other enemies --> default color
                    material.color = Color.blue;

                    if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                    following = EnemyFollowing.RANDOM;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                //if target reached
                if (Vector3.Distance(transform.position, targetTile.gameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;
            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }
}
