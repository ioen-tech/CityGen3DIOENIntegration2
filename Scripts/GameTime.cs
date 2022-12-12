using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameTime : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    private float time = 0f;

    private void Start()
    {
        timeText.SetText(GameManager.Instance.GetGameTimeNow().ToString("ddd d MMMM HH:mm"));
    }

    private void Update()
    {
        time += Time.deltaTime * GameManager.Instance.GetTimeSpeedFactor();
        if (time > 60)
        {
            timeText.SetText(GameManager.Instance.GetGameTimeNow().ToString("ddd d MMMM HH:mm"));
            time = 0;
        }
    }
}
