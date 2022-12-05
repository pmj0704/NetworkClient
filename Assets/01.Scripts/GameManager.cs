using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    const char CHAR_TERMINATOR = ';';
    const char CHAR_COMMA = ',';

    private UserControl userControl;

    [SerializeField]
    private InputField nickname;
    [SerializeField]
    private InputField Chat;
    string myID;
    [SerializeField]
    private float ATTACK_RADIUS = 3.5f;

    public GameObject prefabUser;
    public GameObject User;

    Dictionary<string, UserControl> remoteUsers;
    Queue<string> commandQueue;

    void Start()
    {
        userControl = User.GetComponent<UserControl>();
        remoteUsers = new Dictionary<string, UserControl>();
        commandQueue = new Queue<string>();
    }

    void Update()
    {
        ProcessQueue();
    }

    public void SendCommand(string cmd)
    {
        Debug.Log(SocketModule.GetInstance().name);
        SocketModule.GetInstance().SendData(cmd);
        Debug.Log("cmd send: " + cmd);
    }

    public void QueueCommand(string cmd)
    {
        commandQueue.Enqueue(cmd);
    }

    public void ProcessQueue()
    {
        while (commandQueue.Count > 0)
        {
            string nextCommand = commandQueue.Dequeue();
            ProcessCommand(nextCommand);
        }
    }

    public void ProcessCommand(string cmd)
    {
        bool isMore = true;
        while (isMore)
        {
            Debug.Log("Process cmd: " + cmd);
            //ID
            int nameIdx = cmd.IndexOf('$');
            string id = "";
            if (nameIdx > 0)
            {
                id = cmd.Substring(0, nameIdx);
            }
            //Command
            int cmdIdx1 = cmd.IndexOf('#');
            if (cmdIdx1 > nameIdx)
            {
                int cmdIdx2 = cmd.IndexOf('#', cmdIdx1 + 1);
                if (cmdIdx2 > cmdIdx1)
                {
                    string command = cmd.Substring(cmdIdx1 + 1, cmdIdx2 - cmdIdx1 - 1);
                    //End
                    string remain = "";
                    string nextCommand;
                    int endIdx = cmd.IndexOf(CHAR_TERMINATOR, cmdIdx2 + 1);
                    if (endIdx > cmdIdx2)
                    {
                        remain = cmd.Substring(cmdIdx2, endIdx - cmdIdx2 - 1);
                        nextCommand = cmd.Substring(endIdx + 1);
                    }
                    else
                    {
                        nextCommand = cmd.Substring(cmdIdx2 + 1);
                    }
                    Debug.Log($"command = {command} id = {id} remain = {remain} next ={nextCommand}");

                    if(command == "Attack")
                    {
                        TakeDamage(remain);
                    }

                    if (myID.CompareTo(id) != 0)
                    {
                        switch (command)
                        {
                            case "Enter":
                                AddUser(id);
                                ;
                                break;
                            case "Left":
                                UserLeft(id);
                                ;
                                break;
                            case "Move":
                                SetMove(id, remain);
                                ;
                                break;
                            case "Heal":
                                UserHeal(id);
                                ;
                                break;
                        }
                    }
                    else
                    {
                        Debug.Log("skip");
                    }
                    cmd = nextCommand;
                    if (cmd.Length <= 0)
                    {
                        isMore = false;
                    }
                }
                else
                {
                    isMore = false;

                }
            }
            else
            {
                isMore = false;

            }
        }
    }

    public void Attack()
    {
        Collider2D[] damageUsers = Physics2D.OverlapCircleAll(User.transform.position, ATTACK_RADIUS);
        System.Text.StringBuilder damageString = new System.Text.StringBuilder();

        for (int i = 0; i < damageUsers.Length; i++)
        {
            UserControl userControl = damageUsers[i].GetComponent<UserControl>();
            foreach (var item in remoteUsers)
            {
                if(item.Value == userControl)
                {
                    damageString.Append(item.Key);
                    damageString.Append(',');
                }    
            }
        }

        SendCommand($"#Attack#{damageString.ToString()}");
    }

    public void OnLogin()
    {
        myID = nickname.text;
        if (myID.Length > 0)
        {
            SocketModule.GetInstance().Login(myID);
            User.transform.position = Vector3.zero;
        }
    }

    public void OnLogOut()
    {
        SocketModule.GetInstance().LogOut();
        foreach (var item in remoteUsers)
        {
            Destroy(item.Value.gameObject);
        }
        remoteUsers.Clear();
    }

    public void OnRevive()
    {
        userControl.Revive();
        SendCommand("#Heal#");
    }

    public void OnMessage()
    {
        if (myID != null)
            SocketModule.GetInstance().SendData(Chat.text);
    }

    public void AddUser(string id)
    {
        UserControl uc = null;
        Debug.Log("ADDed");
        if (!remoteUsers.ContainsKey(id))
        {
            Debug.Log("ADD");
            GameObject newUser = Instantiate(prefabUser);
            uc = newUser.GetComponent<UserControl>();
            uc.isRemote = true;
            remoteUsers.Add(id, uc);
        }
    }

    public void UserLeft(string id)
    {
        if (remoteUsers.ContainsKey(id))
        {
            UserControl uc = remoteUsers[id];
            Destroy(uc.gameObject);
            remoteUsers.Remove(id);
        }
    }

    public void UserHeal(string id)
    {
        if (remoteUsers.ContainsKey(id))
        {
            UserControl uc = remoteUsers[id];
            uc.Revive();
        }
    }

    public void TakeDamage(string remain)
    {
        var strs = remain.Split(CHAR_COMMA);
        for (int i = 0; i < strs.Length; i++)
        {
            if (remoteUsers.ContainsKey(strs[i]))
            {
                UserControl uc = remoteUsers[strs[i]];
                if (uc != null) uc.DropHP(10);
            }
            else if (myID == remain)
            {
                userControl.DropHP(10);
            }
        }
    }

    public void SetMove(string id, string cmdMove)
    {
        if (remoteUsers.ContainsKey(id))
        {
            UserControl uc = remoteUsers[id];
            string[] cord = cmdMove.Split(CHAR_COMMA);
            uc.targetPos = new Vector3(float.Parse(cord[0]), float.Parse(cord[1]), 0);
        }
    }
}
