using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using EMBData;

namespace EMBUtility {

	public class PrefabDrop {
		
		public static List<PrefabDrop> UndoList = new List<PrefabDrop> ();

		public GameObject[] prefabs;
		public Vector3 blueBoxPosition;
		public Vector3 blueBoxScale;

		public PrefabDrop (GameObject[] p, Vector3 bbp, Vector3 bbs) {

			prefabs = p;
			blueBoxPosition = bbp;
			blueBoxScale = bbs;

		}

	}

	public struct IntVector3 {

		public int x;
		public int y;
		public int z;

		public IntVector3 (int a, int b, int c) {

			x = a;
			y = b;
			z = c;

		}

		public override bool Equals (object obj) {

			return obj is IntVector3 && this == (IntVector3)obj;

		}

		public override int GetHashCode() {

			return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();

		}

		public static bool operator ==(IntVector3 v1, IntVector3 v2) {

			return (v1.x == v2.x && v1.y == v2.y && v1.z == v2.z);

		}
		
		public static bool operator !=(IntVector3 v1, IntVector3 v2) {
			
			return (v1.x != v2.x || v1.y != v2.y || v1.z != v2.z);
			
		}

		public override string ToString () {

			return string.Format ("IntVector3 ({0}, {1}, {2})", x, y, z);

		}

	}
	
	public struct SmoothTransform {
		
		public Transform root;
		
		private Vector3 p_Position;
		private Quaternion r_Rotation;
		
		public Vector3 position {
			
			set {
				
				startPosTime = (float)EditorApplication.timeSinceStartup;
				startPosition = root.localPosition;
				p_Position = value;
				
			}

			get { return p_Position; }
			
		}
		
		public Quaternion rotation {
			
			set {
				
				startRotTime = (float)EditorApplication.timeSinceStartup;
				startRotation = root.localRotation;
				r_Rotation = value;
				
			}
			
			get { return r_Rotation; }
			
		}
		
		public Vector3 startPosition;
		public Quaternion startRotation;
		
		public float startPosTime;
		public float startRotTime;
		
		public SmoothTransform (Transform r) {
			
			root = r;
			
			startPosTime = -1.0f;
			startRotTime = -1.0f;
			
			startPosition = r.localPosition;
			p_Position = startPosition;
			
			startRotation = r.localRotation;
			r_Rotation = startRotation;
			
		}
		
		public void Move () {
			
			if (root == null)
				return;

			float time = (float)EditorApplication.timeSinceStartup;
			
			if (startPosTime >= 0) {
				
				float t = time - startPosTime;
				
				if (t > 0.5f) {
					
					t = 0.5f;
					startPosTime = -1.0f;
					
				}
				
				root.transform.localPosition = Vector3.Lerp (startPosition, p_Position, t * 2);
				
			}
			
			if (startRotTime >= 0) {
				
				float t = time - startRotTime;
				
				if (t > 0.5f) {
					
					t = 0.5f;
					startRotTime = -1.0f;
					
				}
				
				root.transform.localRotation = Quaternion.Lerp (startRotation, r_Rotation, t * 2);
				
			}
			
		}
		
	}
	
	public struct MousePoint {
		
		public bool exists;
		public Vector3 point;
		
	}

	public static class EMBConversions {

		public static IntVector3 CoordinatesToGrid (Data.MainData mainData, Vector3 coordinates) {

			IntVector3 grid;

			grid.x = Mathf.FloorToInt ((coordinates.x - mainData.OffStep.x + mainData.AxisStep.x / 2) / mainData.AxisStep.x);
			grid.y = Mathf.FloorToInt ((coordinates.y - mainData.OffStep.y) / mainData.AxisStep.y);
			grid.z = Mathf.FloorToInt ((coordinates.z - mainData.OffStep.z + mainData.AxisStep.z / 2) / mainData.AxisStep.z);

			return grid;

		}
		
		public static Vector3 GridToCoordinates (Data.MainData mainData, IntVector3 grid) {

			Vector3 coordinates;
			
			coordinates.x = grid.x * mainData.AxisStep.x + mainData.OffStep.x;
			coordinates.y = grid.y * mainData.AxisStep.y + mainData.OffStep.y;
			coordinates.z = grid.z * mainData.AxisStep.z + mainData.OffStep.z;

			return coordinates;
			
		}

		public static Vector3 CenterCoordinates (Data.MainData mainData, Vector3 coordinates) {
			
			if (mainData.reversed)
				coordinates.y -= 0.005f;

			return GridToCoordinates (mainData, CoordinatesToGrid (mainData, coordinates)) + 0.005f * Vector3.up * (mainData.reversed ? 1 : 0);

		}

	}

	public static class EMBInterface {

		public static Texture2D GetIcon (string name) {

			Texture2D icon = (Texture2D)AssetDatabase.LoadMainAssetAtPath ("Assets/EasyMapBuilder/" + name + ".png");

			if (icon == null)
				Debug.LogWarning ("The Texture Assets/EasyMapBuilder/" + name + ".png is missing. Please consider reimporting EMB or creating one.");

			return icon;

		}
		
		public static Material BuildBoxMat;

		public static Material BlueBuildBoxMat {

			get {

				Material mat = (Material)AssetDatabase.LoadMainAssetAtPath ("Assets/EasyMapBuilder/BlueBuildBoxMaterial.mat");

				if (mat == null)
					Debug.LogWarning ("The Material Assets/EasyMapBuilder/BlueBuildBoxMaterial.mat is missing. Please consider creating one if you need the Build Box.");

				return mat;
			
			}

		}

		public static Material HelpPlaneMat;
		
		public static GameObject CreateBuildBox (Data.MainData mainData, Vector3 position) {
			
			if (BuildBoxMat == null)
				BuildBoxMat = (Material)AssetDatabase.LoadMainAssetAtPath ("Assets/EasyMapBuilder/BuildBoxMaterial.mat");
			
			if (BuildBoxMat == null) {
				
				Debug.LogWarning ("The Material Assets/EasyMapBuilder/BuildBoxMaterial.mat is missing. Please consider creating one if you need the Build Box.");
				return null;
				
			}
			
			GameObject bb = GameObject.CreatePrimitive (PrimitiveType.Cube);

			bb.name = "EMBBuildBox";
			bb.GetComponent<Renderer> ().material = BuildBoxMat;
			bb.transform.localScale = mainData.AxisStep;

			return bb;
			
		}

		public static GameObject CreateHelpPlane (Data.MainData mainData, float height) {
			
			if (!mainData.enableHelpPlane)
				return null;
				
			if (HelpPlaneMat == null)
				HelpPlaneMat = (Material)AssetDatabase.LoadMainAssetAtPath ("Assets/EasyMapBuilder/HelpPlaneMaterial.mat");
			
			if (HelpPlaneMat == null) {

				Debug.LogWarning ("The Material Assets/EasyMapBuilder/HelpPlaneMaterial.mat is missing. Please consider creating one if you need the Help Plane.");
				return null;

			}
				
			GameObject hp = GameObject.CreatePrimitive (PrimitiveType.Plane);
			hp.name = "EMBHelpPlane";
			float px = 10000.0f / mainData.AxisStep.x;
			float py = 10000.0f / mainData.AxisStep.z;
			HelpPlaneMat.mainTextureScale = new Vector2 (px, py);
			Vector2 offset = Vector2.zero;
			offset.x = 1 - (px - Mathf.Floor (px)) / 2.0f + 0.5f * (1 - (Mathf.Floor (px) % 2)) - mainData.OffStep.x / mainData.AxisStep.x;
			offset.y = 1 - (py - Mathf.Floor (py)) / 2.0f + 0.5f * (1 - (Mathf.Floor (py) % 2)) - mainData.OffStep.z / mainData.AxisStep.z;
			HelpPlaneMat.mainTextureOffset = offset;
			hp.GetComponent<Renderer> ().material = HelpPlaneMat;
			hp.transform.position = (height + (mainData.reversed ? 1 : -1) * 0.005f) * Vector3.up;
			hp.transform.localScale = 1000.0f * Vector3.one;

			Object.DestroyImmediate (hp.GetComponent<Collider> ());

			return hp;

		}

		public static void CreateHierarchy (Data.MainData mainData) {
			
			if (mainData.currentMapName != "" && mainData.enableAutoParent) {
				
				GameObject map = GameObject.Find (mainData.currentMapName);
				
				if (map == null) {
					
					map = new GameObject ();
					map.name = mainData.currentMapName;
					map.transform.position = Vector3.zero;
					
				}
				
				for (int j = 0; j < mainData.StartFolder.Count; j++) {
					
					if (mainData.StartFolder [j].GetType () == typeof(EMBFolder) && !map.transform.FindChild (mainData.currentMapName + "_" + mainData.StartFolder [j].name)) {
						
						GameObject category = new GameObject ();
						category.transform.position = Vector3.zero;
						category.transform.parent = map.transform;
						category.name = mainData.currentMapName + "_" + mainData.StartFolder [j].name;
						
					}
					
				}
				
			}

		}

		public static Vector2 DrawPrefabList (Data.MainData mainData, float areaWidth, Vector2 ScrollArea, ref string search, bool editMode = false) {

			Color normalColor = GUI.color;
			
			GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);
			
			GUILayout.BeginVertical ("box");
			
			GUI.color = new Color (0.6f, 1.0f, 0.5f, 1.0f);
			
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Prefabs");

			GUI.enabled = mainData.prefabAmount > 0;

			if (GUILayout.Button (EMBInterface.GetIcon ("RefreshButton"), GUILayout.Width (20))) {

				foreach (EMBAsset a in mainData.StartFolder.ToArray ()) {

					if (a.GetType () == typeof (EMBPrefab) && (a as EMBPrefab).preview == null)
						(a as EMBPrefab).preview = Preview.GetBestPreview ((a as EMBPrefab).prefab);

				}

			}

			GUI.enabled = true;
			
			GUILayout.EndHorizontal ();
			
			if (mainData.StartFolder.Count == 0) {

				GUILayout.FlexibleSpace ();
				GUILayout.EndVertical ();
				return ScrollArea;

			}

			bool isSearching = (search != "");

			search = EditorGUILayout.TextField ("Search : ", search);
			
			ScrollArea = GUILayout.BeginScrollView (ScrollArea);
			
			bool showing = true;
			
			int d = -1;
			
			const int maxWidth = 80;
			const int areaRestriction = 130;
			int index = 0;

			if (isSearching) {
				
				for (int i = 0; i < mainData.StartFolder.Count; i++) {
					
					EMBAsset a = mainData.StartFolder [i];
						
					if (a.name.Contains (search) && a.GetType () == typeof(EMBPrefab)) {
						
						EMBPrefab o = a as EMBPrefab;
						
						if (index == 0) {
							
							GUILayout.BeginHorizontal ();
							GUILayout.FlexibleSpace ();
							
						}
						
						GUI.color = normalColor;
						
						if (editMode) {
							
							if (GUILayout.Button ((o.preview == null ? new GUIContent (o.name) : new GUIContent (o.preview)), GUILayout.Height (maxWidth), GUILayout.Width (maxWidth)))
								mainData.currentBlock = o;
							
						} else {
							
							GUILayout.Box ((o.preview == null ? new GUIContent (o.name) : new GUIContent (o.preview)), GUILayout.Height (maxWidth), GUILayout.Width (maxWidth));
							
							Rect crossRect = GUILayoutUtility.GetLastRect ();
							
							crossRect.x += crossRect.width - 20;
							crossRect.y += crossRect.height - 20;
							crossRect.width = 20;
							crossRect.height = 20;
							
							if (GUI.Button (crossRect, EMBInterface.GetIcon ("RemoveButton"), (o.preview == null ? EditorStyles.miniButtonRight : EditorStyles.miniButton))) {
								
								if (mainData.StartFolder [i] == mainData.currentBlock)
									mainData.currentBlock = null;
								
								mainData.StartFolder.RemoveAt (i);
								
								mainData.prefabAmount -= 1;
								
								EMBAsset a1 = (i == mainData.StartFolder.Count ? null : mainData.StartFolder [i]);
								EMBAsset a2 = mainData.StartFolder [i - 1];
								
								if ((a1 == null || a1.GetType () == typeof(EMBFolder)) && a2.GetType () == typeof(EMBFolder))
									mainData.StartFolder.RemoveAt (i - 1);
								
								Data.SaveData ();
								
								return ScrollArea;
								
							}
							
							crossRect.x -= 20;
							
							if (o.preview == null && GUI.Button (crossRect, EMBInterface.GetIcon ("RefreshButton"), EditorStyles.miniButtonLeft))
								o.preview = Preview.GetBestPreview (o.prefab);
							
						}
						
						index += 1;
						
						if (index * maxWidth > areaWidth - areaRestriction) {
							
							GUILayout.FlexibleSpace ();
							GUILayout.EndHorizontal ();
							index = 0;
							
						}
						
						d -= 1;
					
					}

				}

			} else {
			
				for (int i = 0; i < mainData.StartFolder.Count; i++) {
				
					EMBAsset a = mainData.StartFolder [i];
				
					if (showing == false && a.depth <= d)
						showing = true;
				
					if (showing == true) {
					
						if (a.GetType () == typeof(EMBFolder)) {
						
							EMBFolder o = a as EMBFolder;
						
							if (index != 0) {
							
								GUILayout.FlexibleSpace ();
								GUILayout.EndHorizontal ();
							
								index = 0;
							
							}
						
							if (d > -1 && o.depth < d + 1) {
							
								for (int j = 0; j < d-o.depth+1; j++)
									GUILayout.EndVertical ();
							
							}
						
							d = o.depth;
						
							GUI.color = Color.Lerp (new Color (0.75f, 1.0f, 0.5f), new Color (0.75f, 1.0f, 0.75f), d / 3.0f);
							GUILayout.BeginVertical ("box");
							GUILayout.BeginHorizontal ();
						
							if (GUILayout.Button (o.name, editMode ? EditorStyles.miniButton : EditorStyles.miniButtonLeft, GUILayout.Height (20)))
								o.isShowing = !o.isShowing;
						
							if (!editMode && GUILayout.Button (EMBInterface.GetIcon ("RemoveButton"), EditorStyles.miniButtonRight, GUILayout.Width (20))) {
							
								mainData.StartFolder.RemoveAt (i);
							
								int ind = o.depth;
							
								while (i < mainData.StartFolder.Count && mainData.StartFolder [i].depth > ind) {
								
									if (mainData.StartFolder [i] == mainData.currentBlock)
										mainData.currentBlock = null;
								
									mainData.StartFolder.RemoveAt (i);
									mainData.prefabAmount -= 1;
								
								}

								Data.SaveData ();
							
								return ScrollArea;
							
							}
						
							GUILayout.EndHorizontal ();
						
							if (!o.isShowing)
								showing = false;
						
						} else {
						
							EMBPrefab o = a as EMBPrefab;
						
							d = o.depth;
						
							if (index == 0) {
							
								GUILayout.BeginHorizontal ();
								GUILayout.FlexibleSpace ();
							
							}
						
							GUI.color = normalColor;

							if (editMode) {

								if (GUILayout.Button ((o.preview == null ? new GUIContent (o.name) : new GUIContent (o.preview)), GUILayout.Height (maxWidth), GUILayout.Width (maxWidth)))
									mainData.currentBlock = o;

							} else {
							
								GUILayout.Box ((o.preview == null ? new GUIContent (o.name) : new GUIContent (o.preview)), GUILayout.Height (maxWidth), GUILayout.Width (maxWidth));
							
								Rect crossRect = GUILayoutUtility.GetLastRect ();
							
								crossRect.x += crossRect.width - 20;
								crossRect.y += crossRect.height - 20;
								crossRect.width = 20;
								crossRect.height = 20;
							
								if (GUI.Button (crossRect, EMBInterface.GetIcon ("RemoveButton"), (o.preview == null ? EditorStyles.miniButtonRight : EditorStyles.miniButton))) {
								
									if (mainData.StartFolder [i] == mainData.currentBlock)
										mainData.currentBlock = null;
								
									mainData.StartFolder.RemoveAt (i);

									mainData.prefabAmount -= 1;

									EMBAsset a1 = (i == mainData.StartFolder.Count ? null : mainData.StartFolder [i]);
									EMBAsset a2 = mainData.StartFolder [i - 1];

									if ((a1 == null || a1.GetType () == typeof(EMBFolder)) && a2.GetType () == typeof(EMBFolder))
										mainData.StartFolder.RemoveAt (i - 1);

									Data.SaveData ();
								
									return ScrollArea;
								
								}
							
								crossRect.x -= 20;
							
								if (o.preview == null && GUI.Button (crossRect, EMBInterface.GetIcon ("RefreshButton"), EditorStyles.miniButtonLeft))
									o.preview = Preview.GetBestPreview (o.prefab);

							}
						
							index += 1;
						
							if (index * maxWidth > areaWidth - areaRestriction) {
							
								GUILayout.FlexibleSpace ();
								GUILayout.EndHorizontal ();
								index = 0;
							
							}
						
							d -= 1;
						
						}
					
					}
				
				}

			}
			
			if (index != 0) {
				
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				
				index = 0;
				
			}
			
			if (d > -1) {
				
				for (int j = 0; j < d+1; j++)
					GUILayout.EndVertical ();
				
			}
			
			GUILayout.EndScrollView ();
			
			GUILayout.FlexibleSpace ();
			
			GUILayout.EndVertical ();
			
			return ScrollArea;

		}

	}

}
