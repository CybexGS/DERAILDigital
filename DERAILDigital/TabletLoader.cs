﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using DV.Shops;
using HarmonyLib;
using Object = UnityEngine.Object;
using System.IO;
using DV;
using DV.CabControls;

namespace Cybex.DERAILDigital
{
	public static class TabletLoader
	{
		private static AssetBundle assets;

		public static Action ControllerInstanceCreated;
		public static void OnControllerInstanceCreated() { ControllerInstanceCreated?.Invoke(); }

		public static void Init()
		{
			// not strictly necessary
			_ = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(typeof(TabletLoader).Assembly.Location), "DERAILDigitalEmbedded.dll"));

			//assets = AssetBundle.LoadFromFile(Main.mod?.Path + "tabletcomputer");
			assets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(typeof(TabletLoader).Assembly.Location), "tabletcomputer"));

			CarSpawner.CarSpawned += OnCarSpawned;

			Debug.LogWarning($"[DERAIL Digital] Tablet Loader Init() {assets}");
		}

		public static void OnCarSpawned(TrainCar car)
		{
			if (car.IsLoco)
			{
				GameObject go = null;
				switch (car.name.Split('(')[0])
				{
					case "LocoDiesel":
						go = GameObject.Instantiate(assets.LoadAsset<GameObject>("DS_DE6"), car.interior);
						go.GetComponentInChildren<TabletDockingStation>().trainCar = car;
						break;
					case "loco_621":
						go = GameObject.Instantiate(assets.LoadAsset<GameObject>("DS_DE2"), car.interior);
						go.GetComponentInChildren<TabletDockingStation>().trainCar = car;
						break;
					case "loco_steam_H":
						go = GameObject.Instantiate(assets.LoadAsset<GameObject>("DS_STM"), car.interior);
						go.GetComponentInChildren<TabletDockingStation>().trainCar = car;
						break;
					default:
						break;
				}
				if (go != null)
				{
					go.transform.localPosition = Vector3.zero;
					go.transform.localRotation = Quaternion.identity;
				}


				Debug.Log($"[DERAIL Digital] >>> Spawned {car.name} [{car.ID}]");
			}

			// loco_621
			// LocoDiesel
			// loco_steam_H
		}

		public static void CreateShopItems()
		{
			if (assets == null)
			{
				Debug.LogError("Failed to load tabletcomputer bundle!");
				return;
			}

			Debug.LogWarning($"[DERAIL Digital] Tablet Loader: {assets}");

			InventoryItemSpec itemSpec = assets.LoadAsset<GameObject>("TabletComputer").GetComponent<InventoryItemSpec>();

			Debug.LogWarning($"[DERAIL Digital] Item spec: {itemSpec}");

			GlobalShopController.Instance.shopItemsData.Add(new ShopItemData()
			{
				item = itemSpec,
				basePrice = 1291,
				amount = 1,
				isGlobal = true
			});

			//GlobalShopController.Instance.initialItemAmounts.Add(1);
			((List<int>)typeof(GlobalShopController).GetField("initialItemAmounts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GlobalShopController.Instance)).Add(1);

			Debug.LogWarning("Added global shop data");

			Shop[] shops = Object.FindObjectsOfType<Shop>();
			foreach (Shop shop in shops)
			{
				if (!shop.ToString().Contains("FoodFactory")) continue;
				Debug.LogWarning($"adding to shop {shop}");

				ScanItemResourceModule findMax = shop.scanItemResourceModules.FindMax(r => r.transform.localPosition.x);
				ScanItemResourceModule resource = Object.Instantiate(findMax, findMax.transform.parent);
				resource.gameObject.SetActive(true);
				resource.sellingItemSpec = itemSpec;
				resource.transform.localRotation = findMax.transform.localRotation;
				resource.transform.localPosition = findMax.transform.localPosition + Vector3.right * 1.2f;

				//resource.Start();
				typeof(ScanItemResourceModule).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(resource, null);

				Debug.LogWarning($"new item sign: {resource}");

				var arr = new ScanItemResourceModule[shop.scanItemResourceModules.Length + 1];
				Array.Copy(shop.scanItemResourceModules, 0, arr, 0, shop.scanItemResourceModules.Length);
				arr[arr.Length - 1] = resource;
				shop.cashRegister.resourceMachines = Array.ConvertAll(arr, e => (ResourceModule)e);

				resource.ItemPurchased += GlobalShopController.Instance.AddItemToInstatiationQueue;
			}

			Debug.LogWarning("[DERAIL Digital] Tablet Loader: done");
		}

		[HarmonyPatch]
		class Resources_Patch
		{
			static MethodBase TargetMethod()
			{
				return typeof(Resources).GetMethods().Single(m => m.Name == nameof(Resources.Load) && !m.ContainsGenericParameters && m.GetParameters().Length == 1);
			}

			static bool Prefix(string path, ref Object __result)
			{
				const string prefix = "DERAILDigital.";

				Debug.LogWarning($"Resource load: {path}");

				if (path is { } && assets != null && path.StartsWith(prefix))
				{
					__result = assets.LoadAsset(path.Substring(prefix.Length));
					return false;
				}

				return true;
			}
		}
	}

	public static class Helper
	{
		public static T FindMax<T>(this IEnumerable<T> source, Func<T, float> selector)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (selector == null)
				throw new ArgumentNullException(nameof(selector));
			float f;
			T val;
			using var enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
				throw new InvalidOperationException("Sequence contains no elements");
			for (val = enumerator.Current, f = selector(val); float.IsNaN(f); val = enumerator.Current, f = selector(val))
			{
				if (!enumerator.MoveNext())
					return val;
			}
			while (enumerator.MoveNext())
			{
				var num = selector(enumerator.Current);
				if (num > f)
				{
					f = num;
					val = enumerator.Current;
				}
			}

			return val;
		}
	}
}
