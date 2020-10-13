using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DG2 : MonoBehaviour
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
    [SerializeField]
    [Range(0, 100)]
    private int _exitChance = 50;
    private int _exitCheck = 0;
    private int _roomCheck = 0;
    private bool _reCheck = false;
    [SerializeField]
    private Transform _grave;
    [SerializeField]
    private Transform _levelMap;

    [SerializeField]
    private int _roomLimit;

    [SerializeField]
    [Range(0f,.5f)]
    private float _colliderReSize = 0.1f;

    #region Spin Radius Variables
    private float _spinRadius;
    private BoxCollider _collide;
    private Collider[] _otherColliders;
    #endregion

    [SerializeField]
    private GameObject _playerPrefab;

    private void Start()
    {
        if (_roomCount == 0)
        {
            _currentRoom = SpawnNewRoom(_starterRooms);
            _currentRoom.name = "Start Room";
            PositionRoom(_currentRoom);
            OpenRoomExits(_currentRoom);
            //ManageRoomChildren(_currentRoom, false);
            StartCoroutine(GenerationLoop());
        }
    }

    private IEnumerator GenerationLoop()
    {
        while (_roomCount <= _roomLimit)
        {
            if (_exits.Count > 0 && !_reCheck)
            {
                Debug.Log(_exits.Peek().name + "'s Parent " + _exits.Peek().parent);
                _testingRoom = SpawnNewRoom(_roomPrefabs);
                ManageRoomChildren(_testingRoom, false);
                PositionRoom(_testingRoom);
                yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(.02f);
                RoomCheck(_testingRoom);
            }
            else if (_reCheck)
            {
                yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(.02f);
                RoomCheck(_testingRoom);
            }
            else if (_exits.Count == 0 && _rooms.Count > 0)
            {                
                _currentRoom = _rooms.Dequeue();
                OpenRoomExits(_currentRoom);
            }
            else break;
        }
        while (_exits.Count > 0 && _roomCount >= _roomLimit)
        {
            _exits.Dequeue().GetComponent<ExitInfo>().SetExit(false);
        }
        for (int i = 0; i<_levelMap.childCount; i++)
        {
            ManageRoomChildren(_levelMap.GetChild(i).gameObject, true);
        }

        Instantiate(_playerPrefab, new Vector3(0,1,0), Quaternion.identity);

        Debug.Log("Generation Loop Cmplete with " +_roomCount+" Rooms.");
    }

    private GameObject SpawnNewRoom(GameObject[] RoomPool)
    {
        int RNG = Random.Range(0, RoomPool.Length);
        GameObject Spawn = Instantiate(RoomPool[RNG], this.transform);
        ManageRoomChildren(Spawn, false);
        return Spawn;
    }
    
    private void PositionRoom(GameObject Room)
    {
        if (_roomCount == 0)
        { //This should only fire for the Starter Room
            Room.transform.position = Vector3.zero;
            Room.transform.parent = _levelMap;
            _collide = Room.GetComponent<BoxCollider>();
            _collide.size = new Vector3(_collide.size.x - _colliderReSize, _collide.size.y - _colliderReSize, _collide.size.z - _colliderReSize);
            _roomCount++;
            return;
        }

        if (_exits.Count > 0)
        {
            Debug.Log(Room.name + " picking Exit to Connect.");
            LevelRoom RoomScript = Room.GetComponent<LevelRoom>();
            int RNG = Random.Range(0, RoomScript.Exits.Length);
            _joinedExit = RoomScript.Exits[RNG];
            RoomRotation(_exits.Peek().transform, _joinedExit);
        }
        else
            Debug.Log("No Exits in Queue.");
    }

    private void OpenRoomExits(GameObject Room)
    {//Scan through Current Room's Exits to determine if they will be Used
        Debug.Log("Random Exits for " + Room);
        _currentRoomScript = Room.GetComponent<LevelRoom>();
        for (int i = 0; i < _currentRoomScript.Exits.Length; i++)
        {
            ExitInfo ExitInfo = _currentRoomScript.Exits[i].gameObject.GetComponent<ExitInfo>();
            int RND = Random.Range(0, 100);
            if (!ExitInfo.ExitState() && RND <= _exitChance)
            {
                ExitInfo.SetExit(true);
                _exits.Enqueue(_currentRoomScript.Exits[i]);
            }
        }
    }

    private void ManageRoomChildren(GameObject Parent, bool isActive)
    {
        Debug.Log("Managae Children for " + Parent.name);
        for (int i = 0; i < Parent.transform.childCount; i++)
        {
            Parent.transform.GetChild(i).gameObject.SetActive(isActive);
        }
    }

    private void RoomCheck(GameObject Room)
    {
        bool Check = ColliderCheck(Room);
        if (Check)
        {//Good room
            _roomCheck = 0;
            _exitCheck = 0;
            _rooms.Enqueue(_testingRoom);
            _testingRoom.transform.parent = _levelMap;
            _testingRoom.transform.name = _roomCount.ToString();
            _joinedExit.GetComponent<ExitInfo>().SetExit(true);
            ManageRoomChildren(_testingRoom, false);
            _roomCount++;
            _exits?.Dequeue();
            _reCheck = false;
        }
        else if (_roomCheck <= 5)
        {
            if (_exitCheck <= 5)
            {//Bad Exit try another Exit
                Debug.Log("Bad Exit try another Exit");
                PositionRoom(_testingRoom);
                _exitCheck++;
                _reCheck = true;
            }
            else
            {//Bad Room Respawn another
                Debug.Log("Bad Room Respawn another");
                _testingRoom.transform.position = _grave.position;
                _testingRoom.transform.parent = _grave;
                Destroy(_testingRoom);
                _testingRoom = SpawnNewRoom(_roomPrefabs);
                PositionRoom(_testingRoom);
                _exitCheck = 0;
                _roomCheck++;
                _reCheck = true;
            }
        }
        else
        {//Invalid Exit Close and move on
            Debug.Log("Invalid Exit Close and move on");
            _roomCheck = 0;
            _exitCheck = 0;
            _testingRoom.transform.position = _grave.position;
            _testingRoom.transform.parent = _grave;
            Destroy(_testingRoom.gameObject);
            _exits.Peek().GetComponent<ExitInfo>().SetExit(false);
            _exits.Dequeue();
            _reCheck = false;
        }
    }

    private bool ColliderCheck(GameObject RoomToCheck)
    {
        //returns TRUE if free of Collisions
        _collide = RoomToCheck.GetComponent<BoxCollider>();
        _collide.enabled = true;
        _collide.size = new Vector3(_collide.size.x - _colliderReSize, _collide.size.y - _colliderReSize, _collide.size.z - _colliderReSize);
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
                Debug.Log("Collison Detected. "+RoomToCheck.name +" with " + Other.name);

                return false;
            }
        }
        Debug.Log(RoomToCheck.name + " Passed Collision Check.");
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
            Debug.Log("Problem with Rotation " + (ExitAngle + ExitRoomAngle) + " " + JoinedAngle);
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
