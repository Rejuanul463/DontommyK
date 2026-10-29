using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ClockTiming : MonoBehaviour
{
    public Transform hourDial;
    public Transform minuteDial;
    public Transform SecoundDial;

    public TextMeshProUGUI Hour;
    public TextMeshProUGUI Minute;
    public TextMeshProUGUI AMPM;

    private int counter = 0;
    public int _minute = 0;
    public int _hour = 0;

    public MeshRenderer background;
    public Material[] TimeLapsMaterials;

    public Light light;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(RotateSecoundDial());
        _hour = DateTime.Now.Hour;
        _minute = DateTime.Now.Minute;
        
        SetHourAndMinute(_hour, _minute);
        updateMaterials();
    }

    IEnumerator RotateSecoundDial()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            counter++;
            SecoundDial.Rotate(0,0,6f);

            if (counter == 60)
            {
                counter = 0;
                updateTime();
            }
        }
    }

    private void SetHourAndMinute(int hour, int minute)
    {
        int h = hour % 12;
        minute = minute % 60;
        if (hour >= 12)
        {
            AMPM.text = "PM";
        }
        else
        {
            AMPM.text = "AM";
        }

        float minDep = (minute / 60f) * 30f;
        hourDial.Rotate(0,0, h * 30 + minDep);
        minuteDial.Rotate(0,0, minute * 6);
        if (h == 0)
        {
            h = 12;
        }
        Hour.text = (h > 9) ? h.ToString() : "0" + h.ToString();
        Minute.text = (minute > 9)? minute.ToString() : "0" + minute.ToString();

        _hour = hour;
        _minute = minute;


        if (_hour > 7 && _hour < 17)
        {
            light.intensity = 1;
        }
        else
        {
            light.intensity = 10;
        }
    }

    private void updateTime()
    {
        _minute++;
        if (_minute >= 60)
        {
            _minute = 0;
            _hour++;
        }
        
        int h = _hour % 12;
        if (_hour >= 12)
            AMPM.text = "PM";
        else
            AMPM.text = "AM";
        
        minuteDial.Rotate(0,0, 6);
        float minDep = (_minute / 60f) * 30f;
        hourDial.Rotate(0,0, h * 30 + minDep);
        
        if (h == 0)
        {
            h = 12;
        }
        Hour.text = (h > 9) ? h.ToString() : "0" + h.ToString();
        Minute.text = (_minute > 9)? _minute.ToString() : "0" + _minute.ToString();
        
        if (_hour > 7 && _hour < 17)
        {
            light.intensity = 1;
        }
        else
        {
            light.intensity = 10;
        }
    }

    private void updateMaterials()
    {
        Debug.Log(_hour);
        if (_hour >= 20 || _hour < 4)
        {
            background.material = TimeLapsMaterials[0];
        }else if (_hour >= 4 && _hour < 5)
        {
            background.material = TimeLapsMaterials[1];
        }else if (_hour >= 5 && _hour < 6)
        {
            background.material = TimeLapsMaterials[2];
        }else if (_hour >=6 && _hour < 11)
        {
            background.material = TimeLapsMaterials[3];
        }
        else if(_hour >= 11 && _hour < 15)
        {
            background.material = TimeLapsMaterials[4];
        }else if (_hour >= 15 && _hour < 17)
        {
            background.material = TimeLapsMaterials[5];
        }else if (_hour >= 17 && _hour < 20)
        {
            background.material = TimeLapsMaterials[6];
        }
        else if (_hour >= 18 && _hour < 20)
        {
            background.material = TimeLapsMaterials[7];
        }
    }
}
