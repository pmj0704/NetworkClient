using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserControl : MonoBehaviour
{
    private GameManager gm;

    [SerializeField]
    private float speed = 1f;
    [SerializeField]
    private int MAX_HP = 100;
    [SerializeField]
    private int DROP_HP = 2;
    private Transform orgPos;
    public Vector3 targetPos;
    private int currentHP;
    [SerializeField]
    private float smooth = 2f;

    public Slider slider;

    void Awake()
    {
        orgPos = gameObject.transform;
        targetPos = orgPos.position;
        SetHP(MAX_HP);
    }

    private void Start()
    {
        gm = GetComponent<GameManager>();
        InvokeRepeating("DropHP", 1f, 1f);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if(!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smooth);
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        slider.value = ((float)currentHP /100);
        string moveData = "#Move#" + targetPos.x + ',' + targetPos.y;
        gm.SendCommand(moveData);

        if(Input.GetMouseButtonDown(1))
        {
            string atkData = "#Attack#";
            gm.SendCommand(atkData);
        }
    }

    private void SetTargetPos()
    {
        float distacne = Vector3.Distance(orgPos.position, targetPos);

    }

    private void SetHP(int hp)
    {
        hp = Mathf.Clamp(hp, 0, MAX_HP);
        currentHP = hp;
        slider.value = ((float)currentHP / 100);
    }

    public void DropHP(int drop)
    {
        currentHP -= drop;
        SetHP(drop);
    }

    public void Revive()
    {
        currentHP = MAX_HP;
    }

    public int GetHP()
    {
        return currentHP;
    }
}
