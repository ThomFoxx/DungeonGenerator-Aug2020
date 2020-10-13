using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class DungeonGeneration : MonoBehaviour
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
    private Queue<Transform> _exits = new Queue<Transform>();
    [SerializeField] [Range(0, 100)]
    private int _exitChance = 50;
    private int _exitCheck = 0;
    private int _roomCheck = 0;
    private bool _isChecking = false;
    [SerializeField]
    private GameObject _grave;
    [SerializeField]
    private GameObject _levelMap;

    [SerializeField]
    private int _roomLimit;

    #region Spin Radius Variables
    private float _spinRadius;
    private BoxCollider _collide;
    private Collider[] _otherColliders;
    #endregion

    private void Start()
    {
        StartCoroutine(Generator());
    }

    private IEnumerator Generator()
    {
        while (_roomCount <= _roomLimit)
        {
            if (_roomCount == 0)
            {
                StarterRoomSpawn();
                continue;
            }
            else if (_exits.Count > 0 && _rooms.Count > 0)
                SpawnNewRoom();
            //yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(.1f);
            if (_isChecking)
                RoomCheck();
            if (_testingRoom != null && _testingRoom?.transform.name != "" +(_roomCount-1))
                RoomCheck();
            if (_rooms.Count == 0)
                break;

            if (_grave.transform.childCount > 0) //destroy left over room in this grave
            {
                for (int i = 0; i < _grave.transform.childCount; i++)
                {
                    Destroy(_grave.transform.GetChild(i).gameObject);
                }
            }
        }
    }

    private void StarterRoomSpawn()
    {
        Debug.Log("Spawn StarterRoom");
        #region Spawn Room
        _currentRoom = Instantiate(_starterRooms[Random.Range(0, _starterRooms.Length)], this.transform);
        _currentRoom.transform.parent = _levelMap.transform;
        ManageRoomChildren(_currentRoom, false);
        _currentRoom.name = _roomCount.ToString();
        _roomCount++;
        _rooms.Enqueue(_currentRoom);
        #endregion
        RandomRoomExits();
        MoveToNextExit();
    }

    private void SpawnNewRoom()
    {
        Debug.Log("SpawnNewRoom for " + _currentRoom);
        if (_testingRoom != null && !_rooms.Contains(_testingRoom))
        {
            _testingRoom.transform.parent = _grave.transform;
            _testingRoom.transform.position = _grave.transform.position;
        }
        _testingRoom = Instantiate(_roomPrefabs[Random.Range(0, _roomPrefabs.Length)], this.transform);
        ManageRoomChildren(_testingRoom, false);
        _testingRoomScript = _testingRoom.GetComponent<LevelRoom>();
        PickedExit();
    }

    private void PickedExit()
    {//Picks an exit in the TestingRoom to attached to the current Exit
        Debug.Log("PickExit for " + _currentRoom);
        _joinedExit = _testingRoomScript.Exits[Random.Range(0, _testingRoomScript.Exits.Length - 1)];
        if (_exits.Count > 0)
            RoomRotation(_exits.Peek(), _joinedExit);
    }

    private void RandomRoomExits()
    {//Scan through Current Room's Exits to determine if they will be Used
        Debug.Log("RandomExits for "+_currentRoom);
        _currentRoomScript = _currentRoom.GetComponent<LevelRoom>();
        for (int i = 0; i < _currentRoomScript.Exits.Length; i++)
        {
            ExitInfo ExitInfo = _currentRoomScript.Exits[i].gameObject.GetComponent<ExitInfo>();
            int RND = Random.Range(0, 100);
            if (!ExitInfo.ExitState() && RND <= _exitChance)
            {
                ExitInfo.SetExit(true);
                //_currentRoomScript.Exits[i].gameObject.SetActive(false);
                _exits.Enqueue(_currentRoomScript.Exits[i]);
            }
        }
    }

    private void MoveToNextExit()
    {
        Debug.Log("MoveToNextExit for " + _currentRoom);
        if (_exits.Count == 0)
            return;
        else
        {
            transform.position = _exits.Peek().position;
            return;
        }
    }

    private void ManageRoomChildren(GameObject Parent, bool isActive)
    {
        Debug.Log("ManagaeChildren for " + _currentRoom);
        for (int i = 0; i < Parent.transform.childCount; i++)
        {
            Parent.transform.GetChild(i).gameObject.SetActive(isActive);
        }
    }

    private void RoomCheck()
    {
        if(!_isChecking)
            _isChecking = true;

        if (ColliderCheck(_testingRoom))
        {
            _roomCheck = 0;
            _exitCheck = 0;
            _rooms.Enqueue(_testingRoom);
            _testingRoom.transform.parent = _levelMap.transform;
            _testingRoom.transform.name = _roomCount.ToString();
            ManageRoomChildren(_testingRoom, true);
            _joinedExit.gameObject.SetActive(false);
            _roomCount++;
            _exits?.Dequeue();
            _isChecking = false;
            Cycle();
            return;
        }
        else if (_exitCheck <= 10 && !ColliderCheck(_testingRoom))
        {
            _exitCheck++;
            PickedExit();
            return;
        }
        else if (!ColliderCheck(_testingRoom))
        {
            if (_roomCheck <= 10)
            {
                _roomCheck++;
                _exitCheck = 0;
                _testingRoom.SetActive(false);
                _testingRoom.transform.parent = _grave.transform;
                _testingRoom.transform.position = _grave.transform.position;
                SpawnNewRoom();
                return;
            }
            else if (_roomCheck > 10)
            {
                _roomCheck = 0;
                _testingRoom.SetActive(false);
                _testingRoom.transform.parent = _grave.transform;
                _testingRoom.transform.Translate(Vector3.down * 1000);
                _exits.Dequeue().gameObject.SetActive(true);
                return;
            }
        }
    }

    private void Cycle()
    {
        if (_exits.Count == 0)
        {
            _rooms?.Dequeue();
        }
        else return;

        if (_rooms.Count > 0)
        {
            _currentRoom = _rooms.Peek();
            RandomRoomExits();
            MoveToNextExit();
        }
        else
            StopCoroutine(Generator());
    }

    private bool ColliderCheck(GameObject RoomToCheck)
    {
        //returns TRUE if free of Collisions
        _collide = RoomToCheck.GetComponent<BoxCollider>();
        _collide.enabled = true;
        //_collide.contactOffset = 0.00001f;
        _otherColliders = default;
        GetSpinRadius();

        _otherColliders = Physics.OverlapSphere(RoomToCheck.transform.position, _spinRadius * 2);

        foreach (Collider Other in _otherColliders)
        {
            if (Other.GetType() != typeof(BoxCollider))
                continue;
            if (Other == _collide)
                continue;
            if (Other.transform.parent == _collide.transform)
                continue;
            if (Other.bounds.Intersects(_collide.bounds))
            {
                Debug.Log("Collison Detected. " + Other.name);

                return false;
            }
        }
        return true;
    }

    void GetSpinRadius()
    {//Using Circular Radius with this Project
        #region Circular Radius

        //Calculates Corner to Center for Rotation Clearance on one Axis (Y in this case)
        _spinRadius = Mathf.Sqrt(Mathf.Pow(_collide.bounds.size.x, 2) + Mathf.Pow(_collide.bounds.size.z, 2)) / 2;


        #endregion        
        #region Spherical Radius
        //Calculates Corner to Center for Rotation Clearance on all Axes
        /*
        float c1;
        float c2;

        c1 = Mathf.Sqrt(Mathf.Pow(_collide.bounds.size.x, 2) + Mathf.Pow(_collide.bounds.size.y, 2));
        if (_collide.bounds.size.x < _collide.bounds.size.y)
        {
            c2 = Mathf.Sqrt(Mathf.Pow(_collide.bounds.size.x, 2) + Mathf.Pow(_collide.bounds.size.z, 2));
        }
        else
        {
            c2 = Mathf.Sqrt(Mathf.Pow(_collide.bounds.size.y, 2) + Mathf.Pow(_collide.bounds.size.z, 2));
        }

        SpinRadius = (Mathf.Sqrt(Mathf.Pow(c1, 2) + Mathf.Pow(c2, 2))/2);
        */
        #endregion
    }

    private void RoomRotation(Transform Exit, Transform JoinedExit)
    {//Rotates and Moves Parent Room of JoinedExit to match up with Exit
        Vector3 TempVector = Exit.position;
        Vector3 Offset = JoinedExit.GetComponent<ExitInfo>().offset;

        float TempX = Offset.x;
        float TempZ = Offset.z;
        int TempRot = 0;

        int ExitRoomAngle = GetCardinalDirection(Exit.parent, false);
        int ExitAngle = GetCardinalDirection(Exit, true);
        int JoinedAngle = GetCardinalDirection(JoinedExit, true);

        if (ExitAngle + ExitRoomAngle == 0 || ExitAngle + ExitRoomAngle == 360)
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
        else if (ExitAngle + ExitRoomAngle == 90 || ExitAngle + ExitRoomAngle == 450)
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
        else if (ExitAngle + ExitRoomAngle == 180 || ExitAngle + ExitRoomAngle == 540)
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
        else if (ExitAngle + ExitRoomAngle == 270 || ExitAngle + ExitRoomAngle == 630)
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
            Debug.Log("Problem with Rotation " + (ExitAngle + ExitRoomAngle) + " " +JoinedAngle);
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