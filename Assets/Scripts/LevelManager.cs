using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{

    [SerializeField]
    private GameObject[] _roomPrefabs;
    [SerializeField]
    private GameObject[] _starterRooms;
    private GameObject _currentRoom;
    private LevelRoom _currentRoomScript;
    private LevelRoom _testingRoomScript;
    private GameObject _spawnRoomPrefab;
    private GameObject _testingRoom;
    private Transform _joinedExit;
    private int _roomCount = 0;
    private Queue<GameObject> _rooms = new Queue<GameObject>();
    [SerializeField]
    private int _exitChance = 50;

    #region Spin Radius Variables
    private float SpinRadius;
    private BoxCollider Collide;
    private Collider[] OtherColliders;
    #endregion

    private void Start()
    {
        //Initial Room Spawn
        _currentRoom =Instantiate(_starterRooms[Random.Range(0, _starterRooms.Length - 1)], this.transform);
        _currentRoom.transform.parent = null;
        _rooms.Enqueue(_currentRoom);
        StartCoroutine(ExitSetup());
    }

    private IEnumerator ExitSetup()
    {
        while (_currentRoom != null)
        {
            _currentRoomScript = _currentRoom.GetComponent<LevelRoom>();

            foreach (Transform exit in _currentRoomScript.Exits)
            {
                if (Random.Range(0, 100) >= _exitChance)
                    continue;
                else
                {
                    exit.gameObject.SetActive(false);
                    NewRoom(exit);
                }
            }
            _rooms.Dequeue();
            while (_rooms.Count > 0)
            {
                if (_rooms.Peek() != null)
                {
                    _currentRoom = _rooms.Peek();
                    break;
                }
                else
                    _rooms.Dequeue();
            }
            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    private void NewRoom(Transform exit)
    {
        #region SpawnRandomRoom
        _spawnRoomPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length - 1)];
        _testingRoom = Instantiate(_spawnRoomPrefab, _currentRoom.transform.position, Quaternion.identity);
        _testingRoomScript = _testingRoom.GetComponent<LevelRoom>();
        #endregion

        _joinedExit = _testingRoomScript.Exits[Random.Range(0, _testingRoomScript.Exits.Length - 1)];
        _joinedExit.gameObject.SetActive(false);


        #region MoveRoom To Join Exits
        RoomRotation(exit, _joinedExit);
        float tempRot = (exit.rotation.eulerAngles.y + 180) - (_joinedExit.rotation.eulerAngles.y);
        _testingRoom.transform.rotation = Quaternion.Euler(0, tempRot, 0);
        #endregion

        _rooms.Enqueue(_testingRoom);
    }

    private bool ColliderCheck()
    {        
        Collide = _currentRoom.GetComponent<BoxCollider>();
        Collide.enabled = true;
        GetSpinRadius();

        OtherColliders = Physics.OverlapSphere(_currentRoom.transform.position, SpinRadius*2);

        foreach (Collider Other in OtherColliders)
        {
            if (Other.GetType() != typeof(BoxCollider))
                continue;
            else if (Other == Collide)
                continue;
            else if (Other.bounds.Intersects(Collide.bounds))
            {
                return false;
            }
            else
                return true;
        }
        return true;
    }

    void GetSpinRadius()
    {//Using Circular Radius with this Project
        #region Circular Radius

        //Calculates Corner to Center for Rotation Clearance on one Axis (Y in this case)
        SpinRadius = Mathf.Sqrt(Mathf.Pow(Collide.bounds.size.x, 2) + Mathf.Pow(Collide.bounds.size.z, 2)) / 2;


        #endregion        
        #region Spherical Radius
        //Calculates Corner to Center for Rotation Clearance on all Axes
        /*
        float c1;
        float c2;

        c1 = Mathf.Sqrt(Mathf.Pow(Collide.bounds.size.x, 2) + Mathf.Pow(Collide.bounds.size.y, 2));
        if (Collide.bounds.size.x < Collide.bounds.size.y)
        {
            c2 = Mathf.Sqrt(Mathf.Pow(Collide.bounds.size.x, 2) + Mathf.Pow(Collide.bounds.size.z, 2));
        }
        else
        {
            c2 = Mathf.Sqrt(Mathf.Pow(Collide.bounds.size.y, 2) + Mathf.Pow(Collide.bounds.size.z, 2));
        }

        SpinRadius = (Mathf.Sqrt(Mathf.Pow(c1, 2) + Mathf.Pow(c2, 2))/2);
        */
        #endregion
    }

    private void RoomRotation (Transform Exit, Transform JoinedExit)
    {
        Vector3 TempVector = Exit.position;
        Vector3 Offset = JoinedExit.localPosition;

        float TempX = Offset.x;
        float TempZ = Offset.z;
       
        if (Mathf.Round(Exit.rotation.eulerAngles.y) == 0)
        {
            switch (Mathf.Round(JoinedExit.localRotation.eulerAngles.y))
            {
                case 0:
                    TempVector.x = TempVector.x + TempX;
                    TempVector.z = TempVector.z + TempZ;
                    break;
                case 90:
                    TempVector.x = TempVector.x - TempZ;
                    TempVector.z = TempVector.z + TempX;
                    break;
                case 180:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z - TempZ;
                    break;
                case 270:
                    TempVector.x = TempVector.x + TempZ;
                    TempVector.z = TempVector.z - TempX;
                    break;
                default:
                    Debug.Log("Something is going wrong");
                    break;
            }
        }
        if (Mathf.Round(Exit.rotation.eulerAngles.y) == 90)
        {
            switch (Mathf.Round(JoinedExit.localRotation.eulerAngles.y))
            {
                case 0:
                    TempVector.x = TempVector.x + TempZ;
                    TempVector.z = TempVector.z - TempX;
                    break;
                case 90:
                    TempVector.x = TempVector.x + TempX;
                    TempVector.z = TempVector.z + TempZ;
                    break;
                case 180:
                    TempVector.x = TempVector.x - TempZ;
                    TempVector.z = TempVector.z - TempX;
                    break;
                case 270:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z - TempZ;
                    break;
                default:
                    break;
            }
        }
        if (Mathf.Round(Exit.rotation.eulerAngles.y) == 180 || Mathf.Round(Exit.rotation.eulerAngles.y) == -180)
        {
            switch (Mathf.Round(JoinedExit.localRotation.eulerAngles.y))
            {
                case 0:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z - TempZ;
                    break;
                case 90:
                    TempVector.x = TempVector.x - TempZ;
                    TempVector.z = TempVector.z - TempX;
                    break;
                case 180:
                    TempVector.x = TempVector.x + TempX;
                    TempVector.z = TempVector.z + TempZ;
                    break;
                case 270:
                    TempVector.x = TempVector.x + TempZ;
                    TempVector.z = TempVector.z + TempX;
                    break;
                default:
                    break;
            }
        }
        if (Mathf.Round(Exit.rotation.eulerAngles.y) == 270 || Mathf.Round(Exit.rotation.eulerAngles.y) == -90)
        {
            switch (Mathf.Round(JoinedExit.localRotation.eulerAngles.y))
            {
                case 0:
                    TempVector.x = TempVector.x - TempZ;
                    TempVector.z = TempVector.z + TempX;
                    break;
                case 90:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z - TempZ;
                    break;
                case 180:
                    TempVector.x = TempVector.x + TempZ;
                    TempVector.z = TempVector.z - TempX;
                    break;
                case 270:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z + TempZ;
                    break;
                default:
                    break;
            }
        }
        Debug.Log(Mathf.Round(Exit.rotation.eulerAngles.y));
        Debug.Log(TempVector);
        _testingRoom.transform.position = TempVector; ///New Room Position
    }

}