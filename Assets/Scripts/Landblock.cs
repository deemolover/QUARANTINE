﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Landblock : MonoBehaviour
{

    public Block block;

    /* Following variabls are used for custom initialization.
     * Their values will be referred to in contructor of the block.
     * WARNING: exact property of blocks may be changed during game,
     * making data here invalid. Keep in mind that these are only
     * INITIAL data.
     */
    // number of healthy population in the block
    // will have no effect if random generation flag in VirusModel is set
    public int population;

    // number of infected population in the block
    public int infected;

    // number of virus in the block
    public int virus;

    // number of material in the block
    public int material;

    // determine type of block
    public BlockType type;

    public GameObject[] customOutLandBlocks; 
    

    private bool showInfo;

    // GUI related parameters, used in OnGUI()
    GUIStyle HPStyle;
    GUIStyle IPStyle;
    GUIStyle MStyle;

    GameObject canvas1;
    GameObject canvas2;

    
    // Start is called before the first frame update
    void Start()
    {

        showInfo = false;
        HPStyle = new GUIStyle();
        HPStyle.normal.textColor = new Color(46f / 256f, 163f / 256f, 256f / 256f, 256f / 256f);
        IPStyle = new GUIStyle();
        IPStyle.normal.textColor = Color.red;
        MStyle = new GUIStyle();
        MStyle.normal.textColor = Color.green;

        var canvases = this.GetComponentsInChildren<Canvas>();
        foreach (var ele in canvases)
        {
            if (ele.name == "Canvas") canvas1 = ele.gameObject;
            else canvas2 = ele.gameObject;
        }
        canvas1.SetActive(false);
        canvas2.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        if (block == null) return;
        foreach (Block target in block.outBlocks)
        {
            Vector2 sLoc = block.blockUI.transform.position;
            Vector2 tLoc = target.blockUI.transform.position;
            Debug.DrawLine(sLoc, tLoc, Color.yellow);
        }


    }

    private void OnMouseOver()
    {
        showInfo = true;
    }

    private void OnMouseExit()
    {
        showInfo = false;
    }

    private void OnMouseDown()
    {
        gameObject.SendMessageUpwards("BlockClicked", gameObject);
    }

    private void OnMouseUpAsButton()
    {
        // gameObject.GetComponent<SpriteRenderer>().color = Color.white;
    }

    bool selected = false;
    public void BlockClicked(GameObject obj)
    {
        return;
    }

    public void UpdateSelected(GameObject obj)
    {
        if (obj == gameObject)
        {
            selected = !selected;
        } else
        {
            selected = false;
        }
        if (selected)
            gameObject.GetComponent<SpriteRenderer>().color = Color.gray;
        else
            gameObject.GetComponent<SpriteRenderer>().color = Color.white;
    }

    public void UpdateSprite()
    {
        if (block == null) return;
        if (Utility.GetVirusModel().enableUIVer2)
        {
            SpriteRenderer sr = this.GetComponent<SpriteRenderer>();
            Sprite sprite = null;
            switch (block.type)
            {
                case BlockType.HOSPITAL:
                    sprite = Utility.GetSprite(SpriteType.HOSPITAL);
                    break;
                case BlockType.FACTORY:
                    if (block.IsWorking) sprite = Utility.GetSprite(SpriteType.FACTORY_WORKING);
                    else sprite = Utility.GetSprite(SpriteType.FACTORY_CLOSED);
                    break;
                case BlockType.HOUSING:
                    sprite = Utility.GetSprite(SpriteType.HOUSING);
                    break;
                case BlockType.QUARANTINE:
                    sprite = Utility.GetSprite(SpriteType.QUARANTINE);
                    break;
            }
            if (sprite != null)
            {
                sr.sprite = sprite;
            }
            else Debug.Log("Sprite error!");
        }
    }
    
    private void OnGUI()
    {
        
        if (showInfo)
        {
            GUI.Label(new Rect(Input.mousePosition.x, Screen.height-Input.mousePosition.y-15, 30, 30), block.HPCount.Data.ToString(),HPStyle);
        }
        if (block == null) return;

        if (Utility.GetVirusModel().enableHealthBar)
        {
            canvas2.SetActive(true);
            float volume = block.parameter.POPULATION_VOLUME;

            float hpval = 0, ipval = 0;
            if (Utility.GetVirusModel().enableGodView)
                hpval = block.HPCount.Data;
            else
                hpval = (block.HPCount.Data + block.NIPCount.Data);

            if (hpval > volume) hpval = volume;

            if (Utility.GetVirusModel().enableGodView)
                ipval = (block.CIPCount.Data + block.NIPCount.Data);
            else
                ipval = block.CIPCount.Data;

            if (ipval > volume) ipval = volume;

            foreach (Transform tr in canvas2.transform)
            {
                Vector3 scale = tr.localScale;
                switch (tr.name)
                {
                    case "TotalPop":
                        scale.x = hpval / volume;
                        tr.localScale = scale;
                        break;
                    case "InfectedPop":
                        scale.x = ipval / volume;
                        tr.localScale = scale;
                        break;
                    default:
                        break;
                }
            }
            return;
        }
        canvas1.SetActive(true);
        foreach (Text i in canvas1.GetComponentsInChildren<Text>())
        {
            switch (i.name)
            {
                case "HPUI":
                    if (Utility.GetVirusModel().enableGodView)
                        i.text = block.HPCount.Data.ToString();
                    else
                        i.text = (block.HPCount.Data + block.NIPCount.Data).ToString();
                    break;
                case "CIPUI":
                    if (Utility.GetVirusModel().enableGodView)
                        i.text = (block.CIPCount.Data + block.NIPCount.Data).ToString();
                    else
                        i.text = block.CIPCount.Data.ToString();
                    break;                  
                case "MPUI":
                    i.text = block.MaterialCount.Data.ToString();
                    break;
                default:
                    break;
            }
        }
    }
}
