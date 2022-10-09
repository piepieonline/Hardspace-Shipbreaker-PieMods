using System;
using System.Collections;
using System.Collections.Generic;
using BBI.Unity.Game;
using Carbon.Core;
using Carbon.Core.Events;
using Carbon.Core.Services;
using Carbon.Core.Unity;
using Carbon.Localization.Core;
using Unity.Entities;
using UnityEngine;

namespace ModdedShipLoader
{
    internal class DebugMenu : DebuggerServiceBase, IUse<LocalizationService>, IUse<IEventSystem>
	{
		private Player mPlayer;
		private Collider mPlayerCollider;
		private PlayerMotion mPlayerMotion;

		public override string Name
		{
			get
			{
				return "Shipbuilder Debugger";
			}
		}

		public override void DrawGUI()
		{
			DrawPressureToggle();
			DrawNoClipMode();
		}

		private void DrawPressureToggle()
		{
			if (mPlayer == null)
			{
				mPlayer = GameObject.FindObjectOfType<Player>();
			}

			bool flag = false;
			if (mPlayer != null)
			{
				if (World.DefaultGameObjectInjectionWorld == null)
				{
					return;
				}

				EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
				if (entityManager.Exists(this.mPlayer.Entity))
				{
					Entity entity = this.mPlayer.Entity;
					RoomOccupancy roomOccupancy;
					RoomComponent roomComponent;
					AirPressureState airPressureState;
					if (entityManager.TryGetComponent(entity, out roomOccupancy) && entityManager.Exists(roomOccupancy.CurrentRoom) && entityManager.TryGetComponent(roomOccupancy.CurrentRoom, out roomComponent) && entityManager.TryGetComponent(roomOccupancy.CurrentRoom, out airPressureState) && roomComponent.AllowAirPressureToggle)
					{
						flag = airPressureState.Value == RoomPressurization.Pressurized || airPressureState.Value == RoomPressurization.Decompressed;
						if (flag)
						{
							if (DebugServiceUtils.DebugButton(airPressureState.Value == RoomPressurization.Pressurized ? "Depressurise" : "Pressurise", DebugServiceUtils.sEmptyLayoutOptions))
							{
								UpdateRoomSystem.TryToggleAirPressure(roomOccupancy.CurrentRoom);
							}
						}
					}
				}
				if (!flag)
				{
					GUI.enabled = false;
					DebugServiceUtils.DebugButton("Pressurisation - no room", DebugServiceUtils.sEmptyLayoutOptions);
					GUI.enabled = true;
					return;
				}
			}
			else
			{
				GUI.enabled = false;
				GUILayout.Label("Air Pressure: No Player", DebugServiceUtils.sEmptyLayoutOptions);
				GUI.enabled = true;
			}
		}

		private void DrawNoClipMode()
		{
			if (mPlayer == null)
			{
				mPlayer = GameObject.FindObjectOfType<Player>();
			}
			if (mPlayerMotion == null)
			{
				mPlayerMotion = Player.FindPlayerMotion(mPlayer);
			}
			if (mPlayerCollider == null)
			{
				mPlayerCollider = mPlayer.PlayerCollider;
			}

			if (mPlayerMotion != null && mPlayerCollider != null)
			{
				bool noClipMode = mPlayerCollider.isTrigger;
				if (DebugServiceUtils.DebugButton(noClipMode ? "Turn off no clip" : "Turn on no clip", DebugServiceUtils.sEmptyLayoutOptions))
				{
					Player.SetNoClipMode(mPlayerCollider, mPlayerMotion, !noClipMode);
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
