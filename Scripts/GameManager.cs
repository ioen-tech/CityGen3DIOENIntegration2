using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Range(60, 1800)]
    [SerializeField] private int timeIntervalSeconds = 1800;
    [Range(1, 10000)]
    [SerializeField] private int timeSpeedFactor = 1;
    [Range(1, 12)]
    [SerializeField] private int startMonth = 1;
    [Range(0, 23)]
    [SerializeField] private int startHour = 9;
    private float gameTime;
    private long gameTimeStartTicks;
    private int secondsInDay = 86400;

    private void Awake()
    {
        Instance = this;
        DateTime s = new DateTime(DateTime.Now.Year, startMonth, 1);
        TimeSpan ts = new TimeSpan(startHour, 0, 0);
        s = s.Date + ts;
        gameTimeStartTicks = s.Ticks;
    }

    private void Update()
    {
        if (Time.deltaTime * timeSpeedFactor > timeIntervalSeconds)
        {
            Debug.Log("Time Speed Factor Too fast, reducing it by 20%");
            SetTimeSpeedFactor((int)(timeSpeedFactor * 0.8));
        }
        gameTime += Time.deltaTime * timeSpeedFactor;
    }

    public int GetStartSeconds()
    {
        return startHour * 3600;
    }

    public int GetSecondsInDay()
    {
        return secondsInDay;
    }

    public int GetTimeInterval()
    {
        return timeIntervalSeconds;
    }

    public int GetTimeSpeedFactor()
    {
        return timeSpeedFactor;
    }

    public void SetTimeSpeedFactor(int timeSpeedFactor)
    {
        this.timeSpeedFactor = timeSpeedFactor;
    }

    public DateTime GetGameTimeNow()
    {
        return new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
    }

    public DateTime GetGameTimeTomorrow()
    {
        DateTime gameTimeNow = new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
        TimeSpan ts = new TimeSpan(24, 0, 0);
        gameTimeNow = gameTimeNow.Date + ts;
        return gameTimeNow;
    }

    public int GetMinute()
    {
        DateTime gameTimeNow = new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
        return gameTimeNow.Minute;
    }

    public int GetIntervalOfday()
    {
        DateTime midnight = new DateTime(GetGameTimeNow().Year, GetGameTimeNow().Month, GetGameTimeNow().Day);
        TimeSpan sinceMidnight = GetGameTimeNow() - midnight;
        double secs = sinceMidnight.TotalSeconds;
        int interval = (int)(secs / timeIntervalSeconds);
        return interval;
    }

    public int GetHour()
    {
        DateTime gameTimeNow = new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
        return gameTimeNow.Hour;
    }

    public int GetDay()
    {
        DateTime gameTimeNow = new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
        return gameTimeNow.Day;
    }

    public int GetDayOfYear()
    {
        DateTime gameTimeNow = new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
        return gameTimeNow.DayOfYear;
    }

    public string GetDayOfWeek()
    {
        DateTime gameTimeNow = new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
        return gameTimeNow.DayOfWeek.ToString();
    }

    public int GetMonth()
    {
        DateTime gameTimeNow = new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
        return gameTimeNow.Month;
    }

    public int GetYear()
    {
        DateTime gameTimeNow = new DateTime(gameTimeStartTicks + (long)gameTime * 10000000);
        return gameTimeNow.Year;
    }
}
