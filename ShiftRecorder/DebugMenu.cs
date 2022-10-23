using System;
using System.Collections;
using System.Collections.Generic;
using BBI.Unity.Game;
using Carbon.Core;
using Carbon.Core.Events;
using Carbon.Core.Services;
using Carbon.Core.Unity;
using Carbon.Localization.Core;
using RockVR.Video;
using Unity.Entities;
using UnityEngine;

namespace ShiftRecorder
{
    class DebugMenu : DebuggerServiceBase, IUse<LocalizationService>, IUse<IEventSystem>
	{
		public override string Name
		{
			get
			{
				return "Shift Recorder";
			}
		}

		public static int cameraIndex = 0;

		public override void DrawGUI()
		{
			if (ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl() == null || ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.NOT_START || ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.FINISH)
			{
				if (DebugServiceUtils.DebugButton("Record", Array.Empty<GUILayoutOption>()))
				{
					ShiftRecorder.CreateRecorder();
					ShiftRecorder.StartChangeHud();
					ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.StartCapture();
				}
			}

			if (ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.PAUSED || ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.STARTED)
			{
				if (DebugServiceUtils.DebugButton("End Recording", Array.Empty<GUILayoutOption>()))
				{
					ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.StopCapture();
				}

				if (ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl() != null)
				{
					if (DebugServiceUtils.DebugButton($"{(ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.PAUSED ? "Resume" : "Pause")} Recording", Array.Empty<GUILayoutOption>()))
						ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.ToggleCapture();
				}
			}

			if (DebugServiceUtils.DebugButton("Reload Positions from Settings", Array.Empty<GUILayoutOption>()))
			{
				Settings.Load();
			}

			if (ShiftRecorder.VideoCaptureSession.captureCamera != null)
			{
				foreach(var location in Settings.settings.cameraPositions)
                {
					if (DebugServiceUtils.DebugButton($"Move Camera to {location.name}", Array.Empty<GUILayoutOption>()))
					{
						Settings.MoveCamera(location.name);
						cameraIndex = Settings.settings.cameraPositions.IndexOf(location);
					}
				}

				if (DebugServiceUtils.DebugButton("Move Camera to Current Position", Array.Empty<GUILayoutOption>()))
				{
					ShiftRecorder.VideoCaptureSession.captureCamera.transform.position = LynxCameraController.MainCamera.transform.position;
					ShiftRecorder.VideoCaptureSession.captureCamera.transform.rotation = LynxCameraController.MainCamera.transform.rotation;
				}

				if(ShiftRecorder.VideoCaptureSession.captureCamera != null)
                {
					GUILayout.Label("Current Camera Location", Array.Empty<GUILayoutOption>());
					var pos = ShiftRecorder.VideoCaptureSession.captureCamera.transform.position;
					var rot = ShiftRecorder.VideoCaptureSession.captureCamera.transform.rotation.eulerAngles;
					GUILayout.Label("Position:", Array.Empty<GUILayoutOption>());
					GUILayout.Label($"x: {pos.x}, y: {pos.y} z: {pos.z}", Array.Empty<GUILayoutOption>());
					GUILayout.Label("Rotation:", Array.Empty<GUILayoutOption>());
					GUILayout.Label($"x: {rot.x}, y: {rot.y} z: {rot.z}", Array.Empty<GUILayoutOption>());
				}
			}
		}

		#region Required Inheritance Stuff

		private LocalizationService mLocalizationService;
		private IEventSystem mEventSystem;

		// Token: 0x060043D1 RID: 17361 RVA: 0x00028801 File Offset: 0x00026A01
		void IUse<LocalizationService>.Bind(LocalizationService service)
		{
			this.mLocalizationService = service;
		}

		// Token: 0x060043D2 RID: 17362 RVA: 0x0002880A File Offset: 0x00026A0A
		void IUse<LocalizationService>.Unbind(LocalizationService service)
		{
			this.mLocalizationService = null;
		}

		// Token: 0x060043D3 RID: 17363 RVA: 0x00028813 File Offset: 0x00026A13
		void IUse<IEventSystem>.Bind(IEventSystem service)
		{
			this.mEventSystem = service;
		}

		// Token: 0x060043D4 RID: 17364 RVA: 0x0002881C File Offset: 0x00026A1C
		void IUse<IEventSystem>.Unbind(IEventSystem service)
		{
			this.mEventSystem = null;
		}

        #endregion
    }
}
