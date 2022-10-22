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

		public override void DrawGUI()
		{
			if (ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.NOT_START || ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.FINISH)
			{
				if (DebugServiceUtils.DebugButton("Record", Array.Empty<GUILayoutOption>()))
				{
					ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.StartCapture();
				}
			}

			if (ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.PAUSED || ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.STARTED)
			{
				if (DebugServiceUtils.DebugButton("Stop Recording", Array.Empty<GUILayoutOption>()))
				{
					ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.StopCapture();
				}

				if (ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl != null)
				{
					if (DebugServiceUtils.DebugButton($"{(ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.PAUSED ? "Resume" : "Pause")} Recording", Array.Empty<GUILayoutOption>()))
						ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.ToggleCapture();
				}
			}

			if (ShiftRecorder.VideoCaptureSession.captureCamera != null)
			{
				if (DebugServiceUtils.DebugButton("Move Camera to Current Position", Array.Empty<GUILayoutOption>()))
				{
					ShiftRecorder.VideoCaptureSession.captureCamera.transform.position = LynxCameraController.MainCamera.transform.position;
					ShiftRecorder.VideoCaptureSession.captureCamera.transform.rotation = LynxCameraController.MainCamera.transform.rotation;
				}
			}

			if (DebugServiceUtils.DebugButton("Reload Settings", Array.Empty<GUILayoutOption>()))
			{
				Settings.Load();
			}

			// TODO: Allow choosing of recording locations from settings
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
