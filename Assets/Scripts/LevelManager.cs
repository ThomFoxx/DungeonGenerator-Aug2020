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
    [SerializeField][Range (0,100)]
    private int _exitChance = 50;
    private Quaternion _zeroRot = new Quaternion(0, 0, 0, 0);

    [SerializeField]//debugging tool
    private int _roomLimit;

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
        _currentRoom.name = _roomCount.ToString();
        _roomCount++;
        _rooms.Enqueue(_currentRoom);
        StartCoroutine(ExitSetup());
    }

    private IEnumerator ExitSetup()
    {
        while (_currentRoom != null && _roomCount < _roomLimit)
        {
            _currentRoomScript = _currentRoom.GetComponent<LevelRoom>();

            foreach (Transform exit in _currentRoomScript.Exits)
            {
                if (!exit.gameObject.activeInHierarchy)
                    continue;
                else if (Random.Range(0, 100) > _exitChance)
                    continue;
                else
                {
                    exit.gameObject.SetActive(false);
                    NewRoom(exit);
                }
            }
            _rooms?.Dequeue();
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
        Debug.Log("Generation Complete");
    }

    private void NewRoom(Transform exit)
    {
        int spawncheck = 0;


#region SpawnRandomRoom
Spawn:
        spawncheck++;
        int exitcheck = 0;
        _spawnRoomPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length - 1)];
        _testingRoom = Instantiate(_spawnRoomPrefab, _currentRoom.transform, true);
        _testingRoom.name = _roomCount.ToString();
        _testingRoomScript = _testingRoom.GetComponent<LevelRoom>();
        #endregion

        #region RandomExit
        PickExit:
        exitcheck++;
        _joinedExit = _testingRoomScript.Exits[Random.Range(0, _testingRoomScript.Exits.Length - 1)];
        _joinedExit.gameObject.SetActive(false);
        #endregion

        #region MoveRoom To Join Exits
        RoomRotation(exit, _joinedExit);
        
#endregion

        if (!ColliderCheck(_testingRoom) && exitcheck < 10)
        {
            goto PickExit;
        }
        else if (!ColliderCheck(_testingRoom) && exitcheck > 10)
        {
            if (spawncheck < 10)
                goto Spawn;
            else
            {
                Destroy(_testingRoom.gameObject);
                exit.gameObject.SetActive(true);
            }
        }
        else
        {
            _rooms.Enqueue(_testingRoom);
            _testingRoom.transform.parent = null;
            _testingRoom.transform.name = exit.name + " to " + _joinedExit.name;
            _roomCount++;
        }
    }

    private bool ColliderCheck(GameObject RoomToCheck)
    {      
        //returns TRUE if free of Collisions
        Collide = RoomToCheck.GetComponent<BoxCollider>();
        Collide.enabled = true;
        OtherColliders = default;
        GetSpinRadius();

        OtherColliders = Physics.OverlapSphere(RoomToCheck.transform.position, SpinRadius*2);

        foreach (Collider Other in OtherColliders)
        {
            if (Other.GetType() != typeof(BoxCollider))
                continue;
            else if (Other == Collide)
                continue;
            else if (Other.transform == Collide.transform.parent)
                continue;
            else if (Other.bounds.Intersects(Collide.bounds))
            {
                Debug.Log("Collison Detected. " + Other.name);
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
        //Vector3 Offset = JoinedExit.localPosition;
        Vector3 Offset = JoinedExit.GetComponent<ExitInfo>().offset;
        
        float TempX = Offset.x;
        float TempZ = Offset.z;
        int TempRot = 0;

        int ExitRoomAngle = GetCardinalDirection(Exit.parent, false);
        int ExitAngle = GetCardinalDirection(Exit, true);
        int JoinedAngle = GetCardinalDirection(JoinedExit, true);

        if (ExitAngle + ExitRoomAngle == 0)
        {
            switch (JoinedAngle)
            {
                case 0:
                    TempVector.x = TempVector.x + TempX;
                    TempVector.z = TempVector.z + TempZ;
                    TempRot = 180;// + ExitRoomAngle;
                    break;
                case 90:
                    TempVector.x = TempVector.x - TempZ;
                    TempVector.z = TempVector.z + TempX;
                    TempRot = 90;// + ExitRoomAngle;
                    break;
                case 180:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z - TempZ;
                    TempRot = 0;// + ExitRoomAngle;
                    break;
                case 270:
                    TempVector.x = TempVector.x + TempZ;
                    TempVector.z = TempVector.z - TempX;
                    TempRot = 270;//+ ExitRoomAngle;
                    break;
                default:
                    Debug.Log("Something went Wrong. Rot0");
                    break;
            }
        }
        else if (ExitAngle + ExitRoomAngle == 90)
        {
            switch (JoinedAngle)
            {
                case 0:
                    TempVector.x = TempVector.x + TempZ;
                    TempVector.z = TempVector.z - TempX;
                    TempRot = 270;// + ExitRoomAngle;
                    break;
                case 90:
                    TempVector.x = TempVector.x + TempX;
                    TempVector.z = TempVector.z + TempZ;
                    TempRot = 180;// + ExitRoomAngle;
                    break;
                case 180:
                    TempVector.x = TempVector.x - TempZ;
                    TempVector.z = TempVector.z - TempX;
                    TempRot = 90;// + ExitRoomAngle;
                    break;
                case 270:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z - TempZ;
                    TempRot = 0;// + ExitRoomAngle;
                    break;
                default:
                    Debug.Log("Something went Wrong. Rot90");
                    break;
            }
        }
        else if (ExitAngle + ExitRoomAngle == 180)
        {
            switch (JoinedAngle)
            {
                case 0:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z - TempZ;
                    TempRot = 0;// + ExitRoomAngle;
                    break;
                case 90:
                    TempVector.x = TempVector.x - TempZ;
                    TempVector.z = TempVector.z - TempX;
                    TempRot = 270;// + ExitRoomAngle;
                    break;
                case 180:
                    TempVector.x = TempVector.x + TempX;
                    TempVector.z = TempVector.z + TempZ;
                    TempRot = 180;// + ExitRoomAngle;
                    break;
                case 270:
                    TempVector.x = TempVector.x + TempZ;
                    TempVector.z = TempVector.z + TempX;
                    TempRot = 90;// + ExitRoomAngle;
                    break;
                default:
                    Debug.Log("Something went Wrong. Rot180");
                    break;
            }
        }
        else if (ExitAngle + ExitRoomAngle == 270)
        {
            switch (JoinedAngle)
            {
                case 0:
                    TempVector.x = TempVector.x - TempZ;
                    TempVector.z = TempVector.z + TempX;
                    TempRot = 90;// + ExitRoomAngle;
                    break;
                case 90:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z - TempZ;
                    TempRot = 0;// + ExitRoomAngle;
                    break;
                case 180:
                    TempVector.x = TempVector.x + TempZ;
                    TempVector.z = TempVector.z - TempX;
                    TempRot = 270;//+ ExitRoomAngle;
                    break;
                case 270:
                    TempVector.x = TempVector.x - TempX;
                    TempVector.z = TempVector.z + TempZ;
                    TempRot = 180;// + ExitRoomAngle;
                    break;
                default:
                    Debug.Log("Something went Wrong. Rot270");
                    break;
            }
        }
        else
        {
            Debug.Log("Problem with Rotation " + Mathf.Round(Exit.rotation.eulerAngles.y) + " " + Mathf.Round(JoinedExit.rotation.eulerAngles.y));
        }

        _testingRoom.transform.rotation = Quaternion.Euler(0, TempRot, 0);
        _testingRoom.transform.position = TempVector; ///New Room Position
        
    }

    private int GetCardinalDirection(Transform checkedObject, bool local)
    {
        //Returns a INT angle 'Normalized' to the N,E,S,W on the Y Axis
        float CheckAngle = 0;
        
        if (local)
            CheckAngle = checkedObject.localRotation.eulerAngles.y;
        else
            CheckAngle = checkedObject.rotation.eulerAngles.y;

        int ReturnedAngle = 0;

        if ((CheckAngle <= 45 && CheckAngle >= -45) || (CheckAngle >= 315 && CheckAngle <= 360))
            ReturnedAngle = 0;
        else if (CheckAngle > 45 && CheckAngle <= 135)
            ReturnedAngle = 90;
        else if ((CheckAngle > 135 && CheckAngle <= 225) || (CheckAngle >= -135 && CheckAngle <= -180))
            ReturnedAngle = 180;
        else if ((CheckAngle > 225 && CheckAngle < 315) || (CheckAngle <= -45 && CheckAngle >= 135))
            ReturnedAngle = 270;
        else
            Debug.Log("Problem in Cardinal Check");

        //Debug.Log(checkedObject.name + "'s angle = " + ReturnedAngle, checkedObject);

        return ReturnedAngle;
    }
}