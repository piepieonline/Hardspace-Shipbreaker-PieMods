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

namespace TestProj
{
    internal class DebugMenu : DebuggerServiceBase, IUse<LocalizationService>, IUse<IEventSystem>
	{
		public override string Name
		{
			get
			{
				return "Item Collider Debugger";
			}
		}

		public override void DrawGUI()
		{
			if (DebugServiceUtils.DebugButton("Update colliders", DebugServiceUtils.sEmptyLayoutOptions))
            {
				var grapple = GameObject.Find("PRFB_GrappleGun");
				BoxCollider col;
				if(!grapple.TryGetComponent(out col))
                {
					col = grapple.AddComponent<BoxCollider>();
                }
				col.center = new Vector3(0, .1f, .5f);
				col.size = new Vector3(0.41f, 0.5f, 1);
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
