using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using EMBUtility;

namespace EMBData {

	public class EMBAsset {

		public Object root;
		
		public int depth;
		public string parent;
		public string name;

		public string assetPath;
		
		public EMBAsset () { }

		public EMBAsset (Object r, int d, string p, string n, string path) {

			root = r;

			depth = d;
			parent = p;
			name = n;

			assetPath = path;

		}

	}

	public class EMBFolder : EMBAsset {
		
		public bool isShowing;
		
		public EMBFolder (Object r, int d, string p, string n, string path, bool s = true) {

			root = r;
			
			depth = d;
			parent = p;
			name = n;
			
			assetPath = path;

			isShowing = s;
			
		}
		
		public static EMBFolder Create (EMBAsset a) {
			
			Object asset = AssetDatabase.LoadMainAssetAtPath (a.assetPath);
			
			if (asset != null)
				return new EMBFolder (asset, a.depth, a.parent, a.name, a.assetPath);
			else
				return null;
			
		}
		
		public static EMBFolder Create (Data.SavableEquivalent.SavableEMBFolder folder) {
			
			Object asset = AssetDatabase.LoadMainAssetAtPath (folder.assetPath);
			
			if (asset != null)
				return new EMBFolder (asset, folder.depth, folder.parent, folder.name, folder.assetPath, folder.isShowing);
			else
				return null;
			
		}

	}

	public class EMBPrefab : EMBAsset {
		
		public GameObject prefab;
		public Texture2D preview;
		
		private EMBPrefab (Object r, int d, string p, string n, string path, GameObject g) {
			
			depth = d;
			parent = p;
			name = n;
			
			assetPath = path;

			prefab = g;
			preview = null;

			GetPreview ();
			
		}

		private void GetPreview () {

			preview = Preview.GetBestPreview (prefab);

		}

		public static EMBPrefab Create (EMBAsset a) {
			
			Object asset = AssetDatabase.LoadMainAssetAtPath (a.assetPath);
			
			if (asset != null) {
				
				GameObject g = asset as GameObject;

				if (g == null)
					return null;
				else
					return new EMBPrefab (asset, a.depth, a.parent, a.name, a.assetPath, g);

			} else
				return null;

		}
		
		public static EMBPrefab Create (Data.SavableEquivalent.SavableEMBPrefab prefab) {
			
			Object asset = AssetDatabase.LoadMainAssetAtPath (prefab.assetPath);
			
			if (asset != null) {
				
				GameObject g = asset as GameObject;
				
				if (g == null)
					return null;
				else
					return new EMBPrefab (asset, prefab.depth, prefab.parent, prefab.name, prefab.assetPath, g);
				
			} else
				return null;
			
		}
		
	}

	public static class Preview {
		
		public static Texture2D GetBestPreview (Object asset) {
			
			Texture2D first = AssetPreview.GetAssetPreview (asset);

			if (first == null)
				return null;

			Texture2D best = new Texture2D (first.width, first.height);
			Color[] pix = first.GetPixels ();

			if (CheckPreview (pix)) {

				best.SetPixels (pix);
				best.Apply ();
				return best;

			}
			
			GameObject save = Object.Instantiate (asset) as GameObject;
			GameObject sample = Object.Instantiate (asset) as GameObject;
			
			GameObject empty = new GameObject ();
			empty.transform.SetParent (sample.transform);
			empty.transform.localPosition = Vector3.zero;
			empty.transform.SetParent (null);
			
			sample.transform.SetParent (empty.transform);
			
			Vector3 angles = sample.transform.eulerAngles;
			sample.transform.eulerAngles = new Vector3 (angles.x + 180.0f, angles.y, angles.z);
			
			PrefabUtility.ReplacePrefab (empty, asset, ReplacePrefabOptions.ConnectToPrefab | ReplacePrefabOptions.ReplaceNameBased );
			
			first = AssetPreview.GetAssetPreview (asset);
			pix = first.GetPixels ();
			
			if (CheckPreview (pix)) {
				
				Object.DestroyImmediate (empty);
				
				PrefabUtility.ReplacePrefab (save, asset, ReplacePrefabOptions.ConnectToPrefab | ReplacePrefabOptions.ReplaceNameBased);
				Object.DestroyImmediate (save);
				
				best.SetPixels (pix);
				best.Apply ();
				return best;

			}
			
			sample.transform.eulerAngles = new Vector3 (angles.x, angles.y + 180.0f, angles.z);
			
			PrefabUtility.ReplacePrefab (empty, asset, ReplacePrefabOptions.ConnectToPrefab | ReplacePrefabOptions.ReplaceNameBased );
			
			first = AssetPreview.GetAssetPreview (asset);
			pix = first.GetPixels ();
			
			if (CheckPreview (pix)) {
				
				Object.DestroyImmediate (empty);
				
				PrefabUtility.ReplacePrefab (save, asset, ReplacePrefabOptions.ConnectToPrefab | ReplacePrefabOptions.ReplaceNameBased);
				Object.DestroyImmediate (save);
				
				best.SetPixels (pix);
				best.Apply ();
				return best;
				
			}
			
			sample.transform.eulerAngles = new Vector3 (angles.x, angles.y, angles.z + 180.0f);
			
			PrefabUtility.ReplacePrefab (empty, asset, ReplacePrefabOptions.ConnectToPrefab | ReplacePrefabOptions.ReplaceNameBased );
			
			first = AssetPreview.GetAssetPreview (asset);
			pix = first.GetPixels ();
				
			Object.DestroyImmediate (empty);
			
			PrefabUtility.ReplacePrefab (save, asset, ReplacePrefabOptions.ConnectToPrefab | ReplacePrefabOptions.ReplaceNameBased);
			Object.DestroyImmediate (save);
			
			best.SetPixels (pix);
			best.Apply ();
			return best;
			
		}
		
		private static bool CheckPreview (Color[] pix) {
			
			Color backgroundColor = pix [0];
			
			int i = 0;
			
			while (i < pix.Length) {
				
				if (pix [i] != backgroundColor)
					break;

				i += 1;
				
			}
			
			return (i < pix.Length - 1);
			
		}

	}

	public static class Import {

		public class ImportedAsset {

			public Object asset;
			public string path;

			public ImportedAsset (Object o, string p) {

				asset = o;
				path = p;

			}

		}

		public class EMBAssetType {

			public EMBAsset asset;
			public System.Type type;

			public EMBAssetType (EMBAsset a, System.Type t) {

				asset = a;
				type = t;

			}

		}
		
		private static ImportedAsset[] CustomLoadAssets (string path) {
			
			string datapath = Application.dataPath;
			datapath = datapath.Substring (0, datapath.Length - 6) + path;
			
			string[] results = Directory.GetFiles (datapath);
			
			List<ImportedAsset> assets = new List<ImportedAsset> ();
			
			foreach (string r in results) {
				
				if (r.Substring (r.Length - 7) == ".prefab") {

					string assetPath = path + "/" + r.Substring (datapath.Length + 1);
					assets.Add (new ImportedAsset (AssetDatabase.LoadMainAssetAtPath (assetPath), assetPath));
				
				}

			}
			
			results = Directory.GetDirectories (datapath);
			
			foreach (string r in results) {
				
				string assetPath = path + "/" + r.Substring (datapath.Length + 1);
				assets.Add (new ImportedAsset (AssetDatabase.LoadMainAssetAtPath (assetPath), assetPath));
				
			}
			
			return assets.ToArray ();
			
		}
		
		public static void ImportCurrentFolder (Object Folder) {
			
			if (Folder == null) {
				
				EditorUtility.DisplayDialog("Error", "Please select a valid folder", "Ok");
				return;
				
			}
			
			string FolderPath = AssetDatabase.GetAssetPath (Folder);
			
			string datapath = Application.dataPath;
			datapath = datapath.Substring (0, datapath.Length - 6) + FolderPath;
			
			if (!Directory.Exists (datapath)) {
				
				EditorUtility.DisplayDialog("Error", "Please select a valid folder", "Ok");
				return;
				
			}
			
			if (!EditorUtility.DisplayDialog ("Importing All Prefabs", "Easy Map Builder will import all user-made prefabs inside this directory. It will keep the hierarchy. Do you want to continue ?", "Yes, I'm ready", "Not yet"))
				return;
			
			List<EMBAssetType> ImportedList = new List<EMBAssetType> ();
			
			List<EMBAsset> ToDoList = new List<EMBAsset> ();
			ToDoList.Add (new EMBAsset (Folder, 0, "", Folder.name, FolderPath));

			int total = 0;
			
			while (ToDoList.Count > 0) {
				
				EMBAsset asset = ToDoList [0];
				ToDoList.RemoveAt (0);
				
				datapath = Application.dataPath;
				datapath = datapath.Substring (0, datapath.Length - 6) + asset.assetPath;
				
				if (Directory.Exists (datapath)) {
					
					ImportedAsset[] otherAssets = CustomLoadAssets (asset.assetPath);
					
					if (otherAssets.Length > 0) {
						
						ImportedList.Add (new EMBAssetType (asset, typeof (EMBFolder)));
						int p = 0;
						
						foreach (ImportedAsset o in otherAssets) {

							ToDoList.Insert (p, new EMBAsset (o.asset, asset.depth + 1, asset.name, o.asset.name, o.path));
							p += 1;

						}
						
					}
					
				} else if (PrefabUtility.GetPrefabType (asset.root) == PrefabType.Prefab) {

					ImportedList.Add (new EMBAssetType (asset, typeof (EMBPrefab)));
					total += 1;

				}
				
			}

			int i = 0;
			bool b = false;

			List<EMBAsset> ToAdd = new List<EMBAsset> ();

			while (ImportedList.Count > 0) {
			
				EMBAssetType asset = ImportedList [0];
				ImportedList.RemoveAt (0);

				if (asset.type ==  typeof (EMBFolder))
					ToAdd.Add (EMBFolder.Create (asset.asset));
				else {

					b = EditorUtility.DisplayCancelableProgressBar ("Importing " + Folder.name + "...", "Importing " + asset.asset.name + "...", i * 1.0f / total);
					ToAdd.Add (EMBPrefab.Create (asset.asset));
					
					i += 1;

				}

				if (b)
					break;

			}
			
			EditorUtility.ClearProgressBar ();

			if (!b) {

				Data.data.StartFolder.AddRange (ToAdd);
				Data.data.prefabAmount += total;

			}
			
		}

	}

	public static class Data {

		public class MainData {

			public List<EMBAsset> StartFolder;
			public int prefabAmount;

			public Vector3 AxisStep;
			public Vector3 RotStep;
			public Vector3 OffStep;

			public bool enableHelpPlane;
			public bool enableAutoParent;
			public bool enableAutoSave;

			public float cameraSpeedFactor;
			public int camPerspective;
			public float camCullDistance;
			public float camResolutionRate;
			public bool reversed;

			public string currentMapName;
			
			public EMBPrefab currentBlock;
			
			public void Reset () {
				
				StartFolder =  new List<EMBAsset> ();

				prefabAmount = 0;
				
				AxisStep = 1.0f * Vector3.one;
				RotStep = 4.0f * Vector3.one;
				OffStep = Vector3.zero;
				enableHelpPlane = true;
				enableAutoParent = false;
				enableAutoSave = false;

				cameraSpeedFactor = 2.0f;
				camPerspective = 35;
				camCullDistance = 50;
				camResolutionRate = 1;
				reversed = false;

				currentMapName = "";
				
				currentBlock = null;
				
			}
			
			public bool isDefault {

				get {

					bool b = true;
				
					b = (b && StartFolder.Count == 0);
				
					b = (b && prefabAmount == 0);
				
					b = (b && AxisStep == 1.0f * Vector3.one);
					b = (b && RotStep == 4.0f * Vector3.one);
					b = (b && OffStep == Vector3.zero);
					b = (b && enableHelpPlane == true);
					b = (b && enableAutoParent == false);
					b = (b && enableAutoSave == false);
				
					b = (b && cameraSpeedFactor == 2.0f);
					b = (b && camPerspective == 35);
					b = (b && camCullDistance == 50);
					b = (b && camResolutionRate == 1);
					b = (b && reversed == false);
				
					b = (b && currentMapName == "");

					return b;

				}
				
			}
			
			public MainData () {
				
				Reset ();
				
			}
			
			public MainData (MainData data) {

				if (data == null) {

					Reset ();
					return;

				}
				
				StartFolder =  new List<EMBAsset> ();
				
				foreach (EMBAsset o in data.StartFolder.ToArray ())
					StartFolder.Add (o);

				prefabAmount = data.prefabAmount;
				
				AxisStep = data.AxisStep;
				RotStep = data.RotStep;
				OffStep = data.OffStep;
				enableHelpPlane = data.enableHelpPlane;
				enableAutoParent = data.enableAutoParent;
				enableAutoSave = data.enableAutoSave;

				cameraSpeedFactor = data.cameraSpeedFactor;
				camPerspective = data.camPerspective;
				camCullDistance = data.camCullDistance;
				camResolutionRate = data.camResolutionRate;
				reversed = data.reversed;

				currentMapName = data.currentMapName;
				
				currentBlock = data.currentBlock;
				
			}
			
			public MainData (SavableEquivalent.SavableData data, bool LoadPrefabLibrary) {
				
				StartFolder =  new List<EMBAsset> ();
				prefabAmount = data.prefabAmount;

				if (LoadPrefabLibrary) {

					bool b = false;
					int i = 0;
					
					foreach (SavableEquivalent.SavableEMBAsset a in data.StartFolder.ToArray ()) {
						
						if (a.GetType () == typeof (SavableEquivalent.SavableEMBPrefab)) {
							
							b = EditorUtility.DisplayCancelableProgressBar ("Importing Prefab Library...", "Importing " + a.name + "...", i * 1.0f / prefabAmount);
							EMBPrefab asset = EMBPrefab.Create (a as SavableEquivalent.SavableEMBPrefab);
							
							if (asset != null)
								StartFolder.Add (asset);
							
							i += 1;
							
						} else {
							
							EMBFolder asset = EMBFolder.Create (a as SavableEquivalent.SavableEMBFolder);
							
							if (asset != null)
								StartFolder.Add (asset);
							
						}

						if (b)
							break;
						
					}

					if (b)
						StartFolder = new List<EMBAsset> ();
					
					EditorUtility.ClearProgressBar ();

				}
				
				AxisStep = new Vector3 (data.AxisStep_x, data.AxisStep_y, data.AxisStep_z);
				RotStep = new Vector3 (data.RotStep_x, data.RotStep_y, data.RotStep_z);
				OffStep = new Vector3 (data.OffStep_x, data.OffStep_y, data.OffStep_z);
				enableHelpPlane = data.enableHelpPlane;
				enableAutoParent = data.enableAutoParent;
				enableAutoSave = data.enableAutoSave;

				cameraSpeedFactor = data.cameraSpeedFactor;
				camPerspective = data.camPerspective;
				camCullDistance = data.camCullDistance;
				camResolutionRate = data.camResolutionRate;
				reversed = data.reversed;

				currentMapName = data.currentMapName;
				
				if (data.currentBlock != null)
					currentBlock = EMBPrefab.Create (data.currentBlock);
				else
					currentBlock = null;
				
			}

			public bool Compare (MainData data) {
				
				if (prefabAmount != data.prefabAmount)
					return false;

				if (AxisStep != data.AxisStep)
					return false;
				
				if (RotStep != data.RotStep)
					return false;
				
				if (OffStep != data.OffStep)
					return false;
				
				if (enableHelpPlane != data.enableHelpPlane)
					return false;
				
				if (enableAutoParent != data.enableAutoParent)
					return false;
				
				if (enableAutoSave != data.enableAutoSave)
					return false;
				
				if (cameraSpeedFactor != data.cameraSpeedFactor)
					return false;
				
				if (camPerspective != data.camPerspective)
					return false;
				
				if (camCullDistance != data.camCullDistance)
					return false;
				
				if (camResolutionRate != data.camResolutionRate)
					return false;
				
				if (reversed != data.reversed)
					return false;
				
				if (currentMapName != data.currentMapName)
					return false;
				
				if (currentBlock != data.currentBlock)
					return false;
				
				for (int i = 0; i < StartFolder.ToArray ().Length; i++) {

					if (StartFolder [i] != data.StartFolder [i])
						return false;

				}

				return true;

			}
			
		}

		private static MainData d_Data;

		public static MainData data {

			get {

				if (d_Data == null)
					d_Data = LoadFromFile ();

				return d_Data;

			}

		}
		
		public static class SavableEquivalent {
			
			[System.Serializable]
			public class SavableEMBAsset {
				
				public int depth;
				public string parent;
				public string name;
				
				public string assetPath;
				
			}
			
			[System.Serializable]
			public class SavableEMBFolder : SavableEMBAsset {
				
				public bool isShowing;
				
				public SavableEMBFolder (EMBFolder folder) {
					
					depth = folder.depth;
					parent = folder.parent;
					name = folder.name;
					
					assetPath = folder.assetPath;
					
					isShowing = folder.isShowing;
					
				}
				
			}
			
			[System.Serializable]
			public class SavableEMBPrefab : SavableEMBAsset {
				
				public SavableEMBPrefab (EMBPrefab prefab) {
					
					depth = prefab.depth;
					parent = prefab.parent;
					name = prefab.name;
					
					assetPath = prefab.assetPath;
					
				}
				
			}
			
			[System.Serializable]
			public class SavableData {

				public List<SavableEMBAsset> StartFolder;
				public int prefabAmount;

				public float AxisStep_x;
				public float AxisStep_y;
				public float AxisStep_z;
				public float RotStep_x;
				public float RotStep_y;
				public float RotStep_z;
				public float OffStep_x;
				public float OffStep_y;
				public float OffStep_z;

				public bool enableHelpPlane;
				public bool enableAutoParent;
				public bool enableAutoSave;

				public float cameraSpeedFactor;
				public int camPerspective;
				public float camCullDistance;
				public float camResolutionRate;
				public bool reversed;

				public string currentMapName;

				public SavableEMBPrefab currentBlock;
				
				public SavableData (MainData data) {
					
					StartFolder =  new List<SavableEMBAsset> ();
					
					foreach (EMBAsset a in data.StartFolder.ToArray ()) {
						
						if (a.GetType () == typeof (EMBPrefab))
							StartFolder.Add (new SavableEMBPrefab (a as EMBPrefab));
						else
							StartFolder.Add (new SavableEMBFolder (a as EMBFolder));
						
					}

					prefabAmount = data.prefabAmount;
					
					AxisStep_x = data.AxisStep.x;
					AxisStep_y = data.AxisStep.y;
					AxisStep_z = data.AxisStep.z;
					RotStep_x = data.RotStep.x;
					RotStep_y = data.RotStep.y;
					RotStep_z = data.RotStep.z;
					OffStep_x = data.OffStep.x;
					OffStep_y = data.OffStep.y;
					OffStep_z = data.OffStep.z;
					enableHelpPlane = data.enableHelpPlane;
					enableAutoParent = data.enableAutoParent;
					enableAutoSave = data.enableAutoSave;

					cameraSpeedFactor = data.cameraSpeedFactor;
					camPerspective = data.camPerspective;
					camCullDistance = data.camCullDistance;
					camResolutionRate = data.camResolutionRate;
					reversed = data.reversed;

					currentMapName = data.currentMapName;
					
					if (data.currentBlock != null)
						currentBlock = new SavableEMBPrefab (data.currentBlock);
					else
						currentBlock = null;
					
				}
				
			}
			
		}

		static MainData LoadFromFile () {

			if (!File.Exists (Application.persistentDataPath + "/emb.tqf"))
				return new MainData ();

			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/emb.tqf", FileMode.Open);

			SavableEquivalent.SavableData temp = (SavableEquivalent.SavableData)bf.Deserialize (file);

			MainData datas;

			if (temp.StartFolder.Count > 0)
				datas = new MainData (temp, EditorUtility.DisplayDialog("Prefab Library", "Would you want to load the last prefab library you built in the settings ?", "Yep !", "No thanks !"));
			else
				datas = new MainData (temp, false);

			file.Close();
			return datas;

		}

		public static void SaveData () {
		
			BinaryFormatter bf = new BinaryFormatter ();

			FileStream file = File.Create (Application.persistentDataPath + "/emb.tqf");
			bf.Serialize (file, new SavableEquivalent.SavableData (data));
			file.Close ();
			
		}

	}

}
