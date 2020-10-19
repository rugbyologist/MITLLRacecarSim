﻿using System;
using UnityEngine;

/// <summary>
/// The data which is serialized to disk to persist between simulations.
/// </summary>
[Serializable]
public class SavedData
{
    /// <summary>
    /// The best time information for each winnable level, with indexed by the level WinableIndex.
    /// </summary>
    public BestTimeInfo[] BestTimes;

    /// <summary>
    /// The customization for each car, indexed by car.
    /// </summary>
    public CarCustomization[] CarCustomizations;

    /// <summary>
    /// Returns the default data, which should be used when no saved data can be found.
    /// </summary>
    public static SavedData Default
    {
        get
        {
            SavedData data = new SavedData()
            {
                CarCustomizations = new CarCustomization[]
                {
                    new CarCustomization(Color.white),
                    new CarCustomization(Color.red),
                    new CarCustomization(Color.blue),
                    new CarCustomization(Color.yellow)
                }
            };

            data.ClearBestTimes();
            return data;
        }
    }

    /// <summary>
    /// Reset all best times to store no progress toward any levels.
    /// </summary>
    public void ClearBestTimes()
    {
        this.BestTimes = new BestTimeInfo[LevelInfo.WinableLevels.Count];
        foreach (LevelInfo level in LevelInfo.WinableLevels)
        {
            this.BestTimes[level.WinableIndex] = new BestTimeInfo(level.NumCheckpoints);
        }
    }
}