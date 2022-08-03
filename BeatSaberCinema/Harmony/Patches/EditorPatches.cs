﻿using System.IO;
using BeatmapEditor.Commands;
using BeatmapEditor3D;
using BeatmapEditor3D.BpmEditor;
using BeatmapEditor3D.BpmEditor.Commands;
using BeatmapEditor3D.Controller;
using BeatmapEditor3D.DataModels;
using HarmonyLib;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace BeatSaberCinema.Patches
{
	[HarmonyPatch(typeof(EditBeatmapLevelViewController), "DidActivate")]
	public class EditBeatmapLevelViewControllerDidActivate
	{
		[UsedImplicitly]
		public static void Postfix()
		{
			Log.Debug("EditBeatmapLevelViewControllerDidActivate");
			//PlaybackController.Instance.GameSceneLoaded();
		}
	}

	[HarmonyPatch(typeof(EditBeatmapLevelViewController),"TogglePlayPause")]
	public class EditBeatmapLevelViewControllerPlayPause
	{
		[UsedImplicitly]
		public static void Prefix(ISongPreviewController ____songPreviewController)
		{
			//TODO: This method doesn't seem to be called at all
			if (____songPreviewController.isPlaying)
			{
				PlaybackController.Instance.PauseVideo();
			}
			else
			{
				PlaybackController.Instance.ResumeVideo();
			}
		}
	}

	[HarmonyPatch(typeof(SetPlayHeadCommand), nameof(SetPlayHeadCommand.Execute))]
	public class SetPlayHead
	{
		[UsedImplicitly]
		public static void Prefix(SetPlayHeadSignal ____signal, BpmEditorSongPreviewController ____bpmEditorSongPreviewController)
		{
			var mapTime = AudioTimeHelper.SamplesToSeconds(____signal.sample, ____bpmEditorSongPreviewController.audioClip.frequency);
			PlaybackController.Instance.ResyncVideo(mapTime);
		}
	}

	[HarmonyPatch(typeof(UpdatePlayHeadCommand), nameof(UpdatePlayHeadCommand.Execute))]
	public class UpdatePlayHead
	{
		[UsedImplicitly]
		public static void Prefix(UpdatePlayHeadSignal ____signal, IBeatmapDataModel ____beatmapDataModel)
		{
			//TODO: This triggers not only during seek, but also just normal playback
			var mapTime = AudioTimeHelper.SamplesToSeconds(____signal.sample, ____beatmapDataModel.audioClip.frequency);
			PlaybackController.Instance.ResyncVideo(mapTime);
		}
	}

	[HarmonyPatch(typeof(UpdatePlaybackSpeedCommand), nameof(UpdatePlaybackSpeedCommand.Execute))]
	public class UpdatePlaybackSpeed
	{
		[UsedImplicitly]
		public static void Prefix(UpdatePlaybackSpeedSignal ____signal)
		{
			//TODO: Breaks stuff. May be related to that Unity bug.
			//PlaybackController.Instance.ResyncVideo(null, ____signal.playbackSpeed);
		}
	}

	[HarmonyPatch(typeof(BeatmapProjectManager), nameof(BeatmapProjectManager.SaveBeatmapProject))]
	public class SavingFixPatch
	{
		[UsedImplicitly]
		public static void Prefix()
		{
			PlaybackController.Instance.StopPlayback();
			VideoLoader.StopFileSystemWatcher();
		}
	}

	[HarmonyPatch(typeof(BeatmapProjectManager), nameof(BeatmapProjectManager.SaveBeatmapProject))]
	public class RestoreConfigPatch
	{
		[UsedImplicitly]
		public static void Postfix(string ____lastBackup)
		{
			if (PlaybackController.Instance.VideoConfig != null)
			{
				var config = PlaybackController.Instance.VideoConfig;
				config.NeedsToSave = true;
				VideoLoader.SaveVideoConfig(config);
				if (config.videoFile != null && config.VideoPath != null)
				{
					var path = Path.Combine(____lastBackup, config.videoFile);
					if (File.Exists(path))
					{
						Log.Debug($"Moving {path} to {config.VideoPath}");
						File.Move(path, config.VideoPath);
						VideoLoader.SetupFileSystemWatcher(config.VideoPath);
						PlaybackController.Instance.OnConfigChanged(config, true);
					}
				}
			}
		}
	}
}