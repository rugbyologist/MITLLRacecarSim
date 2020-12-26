﻿using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the autograder-related aspects of an autograder level and run.
/// </summary>
public class AutograderManager : MonoBehaviour
{
    #region Set in Unity Editor
    /// <summary>
    /// The tasks which must be completed for the autograder level, in order.
    /// </summary>
    [SerializeField]
    private AutograderTask[] tasks = new AutograderTask[1];

    /// <summary>
    /// True if we should not continue to the next task until the car stops.
    /// </summary>
    /// <remarks>If the car does not stop before the time limit, no points are deducted.</remarks>
    [SerializeField]
    private bool doNotProceedUntilStopped = false;
    #endregion

    #region Constants
    /// <summary>
    /// The build index of the level which displays a summary of an autograder run.
    /// </summary>
    public const int AutograderSummaryBuildIndex = 25;
    #endregion

    #region Public Interface
    /// <summary>
    /// The score for each completed level in the current autograder run.
    /// </summary>
    public static readonly List<AutograderLevelScore> levelScores = new List<AutograderLevelScore>();
   
    /// <summary>
    /// Reset the autograder for a new lab.
    /// </summary>
    public static void ResetAutograder()
    {
        AutograderManager.levelIndex = 0;
        AutograderManager.levelScores.Clear();
    }

    /// <summary>
    /// Register when a task is successfully completed.
    /// </summary>
    /// <param name="task">The task which was completed.</param>
    public static void CompleteTask(AutograderTask task)
    {
        if (AutograderManager.CurTask == task)
        {
            AutograderManager.instance.levelScore += task.Points;
            AutograderManager.instance.hud.UpdateScore(AutograderManager.instance.levelScore, AutograderManager.LevelInfo.MaxPoints);

            task.Disable();
            AutograderManager.instance.taskIndex++;
            if (AutograderManager.instance.taskIndex >= AutograderManager.instance.tasks.Length)
            {
                if (!AutograderManager.instance.doNotProceedUntilStopped)
                {
                    AutograderManager.instance.FinishLevel();
                }
            }
            else
            {
                AutograderManager.CurTask.Enable();
            }
        }
    }

    /// <summary>
    /// Begin the autograder for a particular level.
    /// </summary>
    /// <param name="hud">The autograder HUD used for this level.</param>
    public void HandleStart(IAutograderHud hud)
    {
        this.startTime = Time.time;
        this.hud = hud;
        this.hud.SetLevelInfo(AutograderManager.levelIndex, AutograderManager.LevelInfo.Title, AutograderManager.LevelInfo.Description);
        this.hud.UpdateScore(this.levelScore, AutograderManager.LevelInfo.MaxPoints);
        this.hud.UpdateTime(0, AutograderManager.LevelInfo.TimeLimit);
    }

    /// <summary>
    /// Handles when an error occurs.
    /// </summary>
    public void HandleError()
    {
        AutograderManager.levelScores.Add(new AutograderLevelScore()
        {
            Score = this.levelScore,
            Time = Time.time - this.startTime ?? Time.time
        });
    }

    /// <summary>
    /// Handles when the user fails the current autograder level.
    /// </summary>
    public void HandleFailure()
    {
        this.FinishLevel();
    }
    #endregion

    /// <summary>
    /// A static reference to the current LevelManager (there is only ever one at a time).
    /// </summary>
    private static AutograderManager instance;

    /// <summary>
    /// The index of the current autograder level in the overall autograder run.
    /// </summary>
    private static int levelIndex = 0;

    /// <summary>
    /// The HUD which displays autograder information.
    /// </summary>
    private IAutograderHud hud;

    /// <summary>
    /// The Time.time at which the autograder started for this level.
    /// </summary>
    private float? startTime = null;

    /// <summary>
    /// The index of the task which must be completed.
    /// </summary>
    private int taskIndex = 0;

    /// <summary>
    /// The total number of points which the user has earned on the current level.
    /// </summary>
    private float levelScore = 0;

    /// <summary>
    /// True when FinishLevel() was called for the current level.
    /// </summary>
    private bool wasFinishedCalled = false;

    /// <summary>
    /// Autograder information about the current level.
    /// </summary>
    private static AutograderLevelInfo LevelInfo { get { return LevelManager.LevelInfo.AutograderLevels[AutograderManager.levelIndex]; } }

    /// <summary>
    /// The current task which must be completed.
    /// </summary>
    private static AutograderTask CurTask { get { return AutograderManager.instance.tasks[AutograderManager.instance.taskIndex]; } }

    private void Awake()
    {
        AutograderManager.instance = this;
    }

    private void Start()
    {
        this.tasks[taskIndex].Enable();
    }

    private void Update()
    {
        if (this.startTime.HasValue)
        {
            float elapsedTime = Time.time - this.startTime.Value;
            this.hud.UpdateTime(elapsedTime, AutograderManager.LevelInfo.TimeLimit);

            if (elapsedTime > AutograderManager.LevelInfo.TimeLimit ||
                (this.doNotProceedUntilStopped && this.taskIndex >= this.tasks.Length && LevelManager.GetCar().Physics.LinearVelocity.magnitude < Constants.MaxStopSeed))
            {
                this.FinishLevel();
            }
        }
    }

    /// <summary>
    /// Handles when the current level is finished, whether successfully or not.
    /// </summary>
    private void FinishLevel()
    {
        // This check is necessary to prevent FinishLevel from being called twice, since it may take multiple frames to load the next level.
        if (!this.wasFinishedCalled)
        {
            this.wasFinishedCalled = true;
            AutograderManager.levelScores.Add(new AutograderLevelScore()
            {
                Score = this.levelScore,
                Time = Time.time - this.startTime ?? Time.time
            });

            AutograderManager.levelIndex++;
            if (AutograderManager.levelIndex == LevelManager.LevelInfo.AutograderLevels.Length)
            {
                LevelManager.FinishAutograder();
            }
            else
            {
                LevelManager.NextAutograderLevel();
            }
        }
        else
        {
            Debug.LogError($"[AutograderManager::FinishLevel] Attempted to call FinishLevel twice for level [{AutograderManager.LevelInfo.Title}], second call ignored.");
        }
    }
}
