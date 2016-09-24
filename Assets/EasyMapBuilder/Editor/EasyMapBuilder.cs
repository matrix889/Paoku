using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EMBUtility;
using EMBData;

// ****    Easy Map Builder version 2.0    ****
// By Ten Quanta

// For informations on how to use Easy Map Builder please read the PDF files in the EasyMapBuilder Folder.

public class EasyMapBuilder : EditorWindow {

	public static EditorWindow PrefabListWindow;

	private Transform editCenter;
	private Camera editCamera;
	private SmoothTransform s_editCenter;
	private SmoothTransform s_editCamera;

	private float editCamSpeed = 1.0f;
	
	private GameObject BuildBox;
	private GameObject HelpPlane;
	private GameObject BlueBuildBox;

	private int BBBIndex;

	private bool selectionExists {

		get {

			return (mainData != null && BBBIndex > -1 && BBBIndex < PrefabDrop.UndoList.Count);

		}

	}

	private Vector2 screenResolution;
	private RenderTexture displayTexture;

	private Vector2 EditModeScroll;
	private Vector2 EditSettingsScroll;
	
	private bool editing = false;

	private GameObject currentBlock;
	private Vector3 currentRot = 0.0f * Vector3.one;
	
	private MousePoint mousePoint;

	private Color currentColor = new Color (0.17f, 0.2f, 0.17f, 1.0f);

	private Texture2D b_BackgroundColor;
	private Texture2D backgroundColor {

		get {

			if (b_BackgroundColor == null) {

				b_BackgroundColor = new Texture2D (1, 1);
				b_BackgroundColor.SetPixel (1, 1, currentColor);
				b_BackgroundColor.Apply ();

			}

			return b_BackgroundColor;

		}

		set { b_BackgroundColor = value; }

	}

	private Data.MainData mainData;

	private int height;

	private int zoomAmount = 5;

	private string search = "";

	[MenuItem ("Window/Ten Quanta/Easy Map Builder")]
	public static void  ShowWindow () {

		EditorWindow window = EditorWindow.GetWindow(typeof(EasyMapBuilder));
		window.position = new Rect (0, 0, 10000, 10000);

		(window as EasyMapBuilder).Initialize ();

	}

	void OnGUI () {

		if (mainData == null) {
			
			if (PrefabListWindow != null)
				PrefabListWindow.Close ();
			
			StopEdit ();
			
			if (editCenter != null) {
				
				s_editCenter.root = null;
				s_editCamera.root = null;
				DestroyImmediate (editCenter.gameObject);
				
			}

			Initialize ();

		}

		Data.MainData tempData = new Data.MainData (mainData);

		GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), backgroundColor, ScaleMode.StretchToFill);
		
		s_editCenter.Move ();
		s_editCamera.Move ();

		Color normalColor = GUI.color;
		GUI.color = new Color (0.3f, 1.0f, 0.3f, 1.0f);
		GUILayout.BeginVertical ("box");
		var centeredStyle = GUI.skin.GetStyle ("Label");
		centeredStyle.alignment = TextAnchor.MiddleCenter;
		centeredStyle.fontStyle = FontStyle.Bold;
		GUILayout.Label ("Easy Map Builder 2.0");
		centeredStyle.fontStyle = FontStyle.Normal;
		GUILayout.EndVertical ();
		
		EditorGUILayout.Space ();

		GUI.color = normalColor;

		Rect windowRect = new Rect (5, 35, 2 * Screen.width / 3 - 10, 50);
		
		GUILayout.BeginArea (windowRect);
		GUILayout.BeginHorizontal ();

		if (editing) {

			if (GUILayout.Button (EMBInterface.GetIcon ("StopButton"), GUILayout.Width (50), GUILayout.Height (50)))
				StopEdit ();

		} else {
			
			if (GUILayout.Button (EMBInterface.GetIcon ("PlayButton"), GUILayout.Width (50), GUILayout.Height (50)))
				StartEdit ();

		}

		Color cache = editing ? Color.white : Color.grey;

		GUILayout.FlexibleSpace ();

		GUI.enabled = editing;

		GUI.color = cache;

		if (GUILayout.Button (EMBInterface.GetIcon ("RotateLeftButton"), GUILayout.Width (50), GUILayout.Height (50)))
			s_editCenter.rotation *= Quaternion.Euler (new Vector3 (0, 90, 0));
		
		if (GUILayout.Button (EMBInterface.GetIcon ("RotateRightButton"), GUILayout.Width (50), GUILayout.Height (50)))
			s_editCenter.rotation *= Quaternion.Euler (new Vector3 (0, -90, 0));
		
		if (GUILayout.Button (EMBInterface.GetIcon ("ZoomInButton"), GUILayout.Width (50), GUILayout.Height (50))) {
			
			zoomAmount = zoomAmount - 1;
			
			if (zoomAmount < 1)
				zoomAmount = 1;
			else
				PlaceCamera ();
			
		}
		
		if (GUILayout.Button (EMBInterface.GetIcon ("ZoomOutButton"), GUILayout.Width (50), GUILayout.Height (50))) {

			zoomAmount = zoomAmount + 1;

			if (zoomAmount > 100)
				zoomAmount = 100;
			else
				PlaceCamera ();

		}

		if (zoomAmount == 5)
			GUI.enabled = false;
		
		if (GUILayout.Button (EMBInterface.GetIcon ("ZoomDefaultButton"), GUILayout.Width (50), GUILayout.Height (50))) {

			zoomAmount = 5;
			PlaceCamera ();
			
		}

		GUI.enabled = editing;
		
		GUILayout.FlexibleSpace ();
		
		int co = PrefabDrop.UndoList.Count;
		
		GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f) * cache;
		
		if (co == 0)
			GUI.enabled = false;
		else
			GUI.enabled = editing;
		
		if (GUILayout.Button (EMBInterface.GetIcon ("UndoButton"), GUILayout.Width (50), GUILayout.Height (50))) {
			
			if (PrefabDrop.UndoList [co - 1] != null) {
				
				foreach (GameObject g in PrefabDrop.UndoList [co - 1].prefabs) {
					
					if (g != null)
						DestroyImmediate (g);
					
				}
				
			}
			
			PrefabDrop.UndoList.RemoveAt (co - 1);
			
			if (PrefabDrop.UndoList.Count > 0 && BlueBuildBox != null && BBBIndex > PrefabDrop.UndoList.Count - 1) {
				
				PrefabDrop d = PrefabDrop.UndoList [PrefabDrop.UndoList.Count - 1];
				BBBIndex = PrefabDrop.UndoList.Count - 1;
				
				BlueBuildBox.transform.position = d.blueBoxPosition;
				BlueBuildBox.transform.localScale = d.blueBoxScale;
				
			} else if (PrefabDrop.UndoList.Count == 0 && BlueBuildBox)
				DestroyImmediate (BlueBuildBox);
			
		}
		
		GUI.color = new Color (0.7f, 0.9f, 0.6f, 1.0f) * cache;
		
		GUI.enabled = editing;
		
		if (BBBIndex < 0 || BBBIndex > PrefabDrop.UndoList.Count - 1)
			GUI.enabled = false;
		else
			GUI.enabled = editing;
		
		if (GUILayout.Button (EMBInterface.GetIcon ("DeleteButton"), GUILayout.Width (50), GUILayout.Height (50))) {

			PrefabDrop d = PrefabDrop.UndoList [BBBIndex];

			foreach (GameObject g in d.prefabs)
				DestroyImmediate (g);

			PrefabDrop.UndoList.RemoveAt (BBBIndex);
			
			if (PrefabDrop.UndoList.Count > 0) {

				BBBIndex -= 1;

				if (BBBIndex < 0)
					BBBIndex = 0;

				d = PrefabDrop.UndoList [BBBIndex];

				if (BlueBuildBox != null) {
				
					BlueBuildBox.transform.position = d.blueBoxPosition;
					BlueBuildBox.transform.localScale = d.blueBoxScale;

				}
				
			} else if (PrefabDrop.UndoList.Count == 0 && BlueBuildBox)
				DestroyImmediate (BlueBuildBox);
		
		}

		if (mainData.currentBlock == null)
			GUI.enabled = false;
		else
			GUI.enabled = editing;
		
		if (GUILayout.Button (EMBInterface.GetIcon ("ClearButton"), GUILayout.Width (50), GUILayout.Height (50)))
			mainData.currentBlock = null;

		GUI.enabled = editing;
		
		if (GUILayout.Button (EMBInterface.GetIcon ("MirrorButton"), GUILayout.Width (50), GUILayout.Height (50))) {
			
			mainData.reversed = !mainData.reversed;
			
			if (HelpPlane)
				HelpPlane.transform.position = new Vector3 (0, height * mainData.AxisStep.y + (mainData.reversed ? 1 : -1) * 0.005f + mainData.OffStep.y, 0);
			
			PlaceCamera ();
			
		}

		if (BBBIndex <= 0 || PrefabDrop.UndoList.Count == 0)
			GUI.enabled = false;
		else
			GUI.enabled = editing;
		
		if (GUILayout.Button (EMBInterface.GetIcon ("PreviousButton"), GUILayout.Width (50), GUILayout.Height (50)))
			PreviousPrefabDrop ();
		
		if (BBBIndex >= PrefabDrop.UndoList.Count - 1 || PrefabDrop.UndoList.Count == 0)
			GUI.enabled = false;
		else
			GUI.enabled = editing;
		
		if (GUILayout.Button (EMBInterface.GetIcon ("NextButton"), GUILayout.Width (50), GUILayout.Height (50)))
			NextPrefabDrop ();
		
		if (BBBIndex > PrefabDrop.UndoList.Count - 1 || BBBIndex < 0 || PrefabDrop.UndoList.Count == 0)
			GUI.enabled = false;
		else
			GUI.enabled = editing;
		
		if (GUILayout.Button (EMBInterface.GetIcon ("AppleButton"), GUILayout.Width (50), GUILayout.Height (50)))
			ClampCollision (false);
		
		if (GUILayout.Button (EMBInterface.GetIcon ("AppleNormalButton"), GUILayout.Width (50), GUILayout.Height (50)))
			ClampCollision (true);
		
		GUI.color = normalColor;
		GUILayout.EndHorizontal ();
		GUI.enabled = true;
		GUILayout.EndArea ();

		windowRect = new Rect (5, 98, 2 * Screen.width / 3 - 10, Screen.height - 128);
		
		if (editCamera.targetTexture == null || screenResolution.x != windowRect.width * mainData.camResolutionRate || screenResolution.y != windowRect.height * mainData.camResolutionRate) {
			
			screenResolution.x = windowRect.width * mainData.camResolutionRate;
			screenResolution.y = windowRect.height * mainData.camResolutionRate;
			displayTexture = new RenderTexture (Mathf.FloorToInt (screenResolution.x), Mathf.FloorToInt (screenResolution.y), 24);
			editCamera.targetTexture = displayTexture;
			
		}

		if (editing)
			Build (windowRect);

		editCamera.Render ();

		if (!editing)
			GUI.color = Color.grey;

		GUI.Box (windowRect, "");

		windowRect.x += 5;
		windowRect.y += 5;
		windowRect.width -= 10;
		windowRect.height -= 10;
		
		GUI.DrawTexture (windowRect, displayTexture, ScaleMode.StretchToFill);

		windowRect = new Rect (2 * Screen.width / 3 + 5, 35, Screen.width / 3 - 10, Screen.height - 65);

		GUILayout.BeginArea (windowRect);

		if (editing) {

			GUILayout.BeginHorizontal ();
			
			GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);
			
			GUILayout.BeginVertical ("box");

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Current Position");

			GUI.color = normalColor;
			
			if (GUILayout.Button ("Randomize", EditorStyles.miniButtonLeft))
				RandomizeLastPosition (true, true, true);
			
			if (GUILayout.Button ("Reset", EditorStyles.miniButtonRight))
				ResetLastPosition (true, true, true);

			GUILayout.EndHorizontal ();
			
			GUI.color = new Color (1.0f, 0.6f, 0.6f, 1.0f);
			
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("X");
			GUILayout.FlexibleSpace ();
			GUILayout.Label ((mousePoint.exists ? (Mathf.Floor (mousePoint.point.x * 1000.0f) / 1000.0f).ToString () : "n/a"));
			GUILayout.FlexibleSpace ();

			if (GUILayout.Button ("R", EditorStyles.miniButtonLeft, GUILayout.Width (20)))
				RandomizeLastPosition (true, false, false);
			
			if (GUILayout.Button ("0", EditorStyles.miniButtonRight, GUILayout.Width (20)))
				ResetLastPosition (true, false, false);
			
			GUILayout.EndHorizontal ();
			
			GUI.color = new Color (0.6f, 1.0f, 0.6f, 1.0f);
			
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Y");
			GUILayout.FlexibleSpace ();
			GUILayout.Label ((mousePoint.exists ? (Mathf.Floor (mousePoint.point.y * 1000.0f) / 1000.0f).ToString () : "n/a"));
			GUILayout.FlexibleSpace ();

			if (GUILayout.Button ("R", EditorStyles.miniButtonLeft, GUILayout.Width (20)))
				RandomizeLastPosition (false, true, false);
			
			if (GUILayout.Button ("0", EditorStyles.miniButtonRight, GUILayout.Width (20)))
				ResetLastPosition (false, true, false);
			
			GUILayout.EndHorizontal ();
			
			GUI.color = new Color (0.6f, 0.6f, 1.0f, 1.0f);
			
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Z");
			GUILayout.FlexibleSpace ();
			GUILayout.Label ((mousePoint.exists ? (Mathf.Floor (mousePoint.point.z * 1000.0f) / 1000.0f).ToString () : "n/a"));
			GUILayout.FlexibleSpace ();

			if (GUILayout.Button ("R", EditorStyles.miniButtonLeft, GUILayout.Width (20)))
				RandomizeLastPosition (false, false, true);
			
			if (GUILayout.Button ("0", EditorStyles.miniButtonRight, GUILayout.Width (20)))
				ResetLastPosition (false, false, true);
			
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();
			
			GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);
			
			GUILayout.BeginVertical ("box");
			
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Current Rotation");
			
			GUI.color = normalColor;
			
			if (GUILayout.Button ("Randomize", EditorStyles.miniButtonLeft))
				RandomizeLastRotation (true, true, true);
			
			if (GUILayout.Button ("Reset", EditorStyles.miniButtonRight))
				ResetLastRotation (true, true, true);
			
			GUILayout.EndHorizontal ();
			
			GUI.color = new Color (1.0f, 0.6f, 0.6f, 1.0f);
			
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("X");
			GUILayout.FlexibleSpace ();
			GUILayout.Label ("" + Mathf.Round (currentRot.x * 100.0f) / 100.0f);
			GUILayout.FlexibleSpace ();
			
			if (GUILayout.Button ("R", EditorStyles.miniButtonLeft, GUILayout.Width (20)))
				RandomizeLastRotation (true, false, false);
			
			if (GUILayout.Button ("0", EditorStyles.miniButtonRight, GUILayout.Width (20)))
				ResetLastRotation (true, false, false);
			
			GUILayout.EndHorizontal ();
			
			GUI.color = new Color (0.6f, 1.0f, 0.6f, 1.0f);
			
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Y");
			GUILayout.FlexibleSpace ();
			GUILayout.Label ("" + Mathf.Round (currentRot.y * 100.0f) / 100.0f);
			GUILayout.FlexibleSpace ();
			
			if (GUILayout.Button ("R", EditorStyles.miniButtonLeft, GUILayout.Width (20)))
				RandomizeLastRotation (false, true, false);
			
			if (GUILayout.Button ("0", EditorStyles.miniButtonRight, GUILayout.Width (20)))
				ResetLastRotation (false, true, false);
			
			GUILayout.EndHorizontal ();
			
			GUI.color = new Color (0.6f, 0.6f, 1.0f, 1.0f);
			
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Z");
			GUILayout.FlexibleSpace ();
			GUILayout.Label ("" + Mathf.Round (currentRot.z * 100.0f) / 100.0f);
			GUILayout.FlexibleSpace ();
			
			if (GUILayout.Button ("R", EditorStyles.miniButtonLeft, GUILayout.Width (20)))
				RandomizeLastRotation (false, false, true);
			
			if (GUILayout.Button ("0", EditorStyles.miniButtonRight, GUILayout.Width (20)))
				ResetLastRotation (false, false, true);
			
			GUILayout.EndHorizontal ();
			
			GUILayout.EndVertical ();
			
			GUILayout.EndHorizontal ();
			
			EditorGUILayout.Space ();

			GUI.color = normalColor;

			EditModeScroll = EMBInterface.DrawPrefabList (mainData, windowRect.width, EditModeScroll, ref search, true);

			Repaint ();

		} else {

			GUI.color = new Color (0.6f, 1.0f, 0.5f, 1.0f);

			if (GUILayout.Button ("Edit Item Selection", GUILayout.Height (50))) {

				EasyMapBuilderPrefabList.ShowWindow ();
				
				EditorWindow window = EditorWindow.GetWindow (typeof(EasyMapBuilderPrefabList));
				window.position = new Rect (0, 0, 500, 10000);

			}

			EditorGUILayout.Space ();

			EditSettingsScroll = GUILayout.BeginScrollView (EditSettingsScroll);
			
			GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);

			GUILayout.BeginVertical ("box");
			
			GUI.color = new Color (0.6f, 1.0f, 0.5f, 1.0f);
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Position Step (in unit)");
			GUILayout.EndHorizontal ();

			GUI.color = new Color (1.0f, 0.6f, 0.6f, 1.0f);
			mainData.AxisStep.x = Mathf.Clamp (EditorGUILayout.FloatField ("X Step : ", mainData.AxisStep.x), 0.01f, Mathf.Infinity);
			
			GUI.color = new Color (0.6f, 1.0f, 0.6f, 1.0f);
			mainData.AxisStep.y = Mathf.Clamp (EditorGUILayout.FloatField ("Y Step : ", mainData.AxisStep.y), 0.01f, Mathf.Infinity);
			
			GUI.color = new Color (0.6f, 0.6f, 1.0f, 1.0f);
			mainData.AxisStep.z = Mathf.Clamp (EditorGUILayout.FloatField ("Z Step : ", mainData.AxisStep.z), 0.01f, Mathf.Infinity);
			GUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			
			GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);
			
			GUILayout.BeginVertical ("box");
			
			GUI.color = new Color (0.6f, 1.0f, 0.5f, 1.0f);
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Rotation Step (in fraction of 360°)");
			GUILayout.EndHorizontal ();
			
			GUI.color = new Color (1.0f, 0.6f, 0.6f, 1.0f);
			mainData.RotStep.x = Mathf.Clamp (EditorGUILayout.IntField ("Pitch (X) : ", (int)mainData.RotStep.x), 1, Mathf.Infinity);
			
			GUI.color = new Color (0.6f, 1.0f, 0.6f, 1.0f);
			mainData.RotStep.y = Mathf.Clamp (EditorGUILayout.IntField ("Yaw (Y) : ", (int)mainData.RotStep.y), 1, Mathf.Infinity);
			
			GUI.color = new Color (0.6f, 0.6f, 1.0f, 1.0f);
			mainData.RotStep.z = Mathf.Clamp (EditorGUILayout.IntField ("Roll (Z) : ", (int)mainData.RotStep.z), 1, Mathf.Infinity);
			GUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			
			GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);
			
			GUILayout.BeginVertical ("box");
			
			GUI.color = new Color (0.6f, 1.0f, 0.5f, 1.0f);
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Grid Offsteps (in unit)");
			GUILayout.EndHorizontal ();
			
			GUI.color = new Color (1.0f, 0.6f, 0.6f, 1.0f);
			mainData.OffStep.x = Mathf.Clamp (EditorGUILayout.FloatField ("Offstep X : ", mainData.OffStep.x), 0.0f, mainData.AxisStep.x);
			
			GUI.color = new Color (0.6f, 1.0f, 0.6f, 1.0f);
			mainData.OffStep.y = Mathf.Clamp (EditorGUILayout.FloatField ("Offstep Y : ", mainData.OffStep.y), 0.0f, mainData.AxisStep.y);
			
			GUI.color = new Color (0.6f, 0.6f, 1.0f, 1.0f);
			mainData.OffStep.z = Mathf.Clamp (EditorGUILayout.FloatField ("Offstep Z : ", mainData.OffStep.z), 0.0f, mainData.AxisStep.z);
			GUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			
			GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);

			GUILayout.BeginVertical ("box");
			
			GUI.color = new Color (0.6f, 1.0f, 0.5f, 1.0f);
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Camera Settings");
			GUILayout.EndHorizontal ();
			
			GUI.color = normalColor;

			mainData.cameraSpeedFactor = EditorGUILayout.Slider ("Camera Speed Factor : ", mainData.cameraSpeedFactor, 1.0f, 5.0f);

			int p = mainData.camPerspective;
			mainData.camPerspective = EditorGUILayout.IntSlider ("Camera Perspective : ", mainData.camPerspective, 1, 90);

			if (mainData.camPerspective != p)
				PlaceCamera ();

			mainData.camCullDistance = EditorGUILayout.Slider ("Camera Culling Distance : ", mainData.camCullDistance, 5, 100);

			mainData.camResolutionRate = EditorGUILayout.Slider ("Camera Resolution Rate : ", mainData.camResolutionRate, 0.1f, 1);

			GUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			
			GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);
			
			GUILayout.BeginVertical ("box");
			
			GUI.color = new Color (0.6f, 1.0f, 0.5f, 1.0f);
			GUILayout.BeginHorizontal ("box");
			GUILayout.Label ("Other Settings");
			GUILayout.EndHorizontal ();

			GUI.color = normalColor;
			
			mainData.enableHelpPlane = EditorGUILayout.Toggle ("Enable Grid : ", mainData.enableHelpPlane);

			mainData.enableAutoParent = EditorGUILayout.Toggle ("Enable Auto Parenting : ", mainData.enableAutoParent);

			if (mainData.enableAutoParent)
				mainData.currentMapName = (string)EditorGUILayout.TextField ("Current Edited Map : ", mainData.currentMapName);

			mainData.enableAutoSave = EditorGUILayout.Toggle ("Enable Auto Save : ", mainData.enableAutoSave);
			
			GUILayout.EndVertical ();

			GUILayout.EndScrollView ();

			GUILayout.FlexibleSpace ();

			GUI.color = Color.red;

			GUI.enabled = !mainData.isDefault;
			
			if (GUILayout.Button ("Reset All Settings", GUILayout.Height (50))) {
				
				if (EditorUtility.DisplayDialog ("Reseting all settings", "Are you sure you want to reset all settings ? This includes all imported prefab libraries. You can't undo this action.", "Yes, I'm sure !", "Actually no"))
					mainData.Reset ();

			}
			
		}

		GUILayout.EndArea ();

		editCamera.farClipPlane = Mathf.Max (mainData.AxisStep.x, mainData.AxisStep.y, mainData.AxisStep.z) * mainData.camCullDistance * zoomAmount / 5.0f;

		if (!mainData.Compare(tempData))
			Data.SaveData ();

	}

	private bool MouseClicked = false;
	private Dictionary<IntVector3, GameObject> currentlyDropped = new Dictionary<IntVector3, GameObject> ();
	private IntVector3 startPoint;
	private IntVector3 lastPoint;

	private float timer = (float)EditorApplication.timeSinceStartup;

	public void Build (Rect windowRect) {

		Event e = Event.current;
		Rect boundsRect = new Rect (2, 2, Screen.currentResolution.width - 4, Screen.currentResolution.height - 4);
		Vector2 screenPoint;
		
		screenPoint = e.mousePosition - (new Vector2 (windowRect.x + windowRect.width / 2.0f, windowRect.y + windowRect.height / 2.0f));
		screenPoint = new Vector2 (screenPoint.x / windowRect.width, -screenPoint.y / windowRect.height);
		
		int x = 0;
		int z = 0;
		int sprint = 1;

		/*if (e.type == EventType.KeyDown) {

			x = (e.keyCode == KeyCode.A ? -1 : 0) + (e.keyCode == KeyCode.D ? 1 : 0);
			z = (e.keyCode == KeyCode.S ? -1 : 0) + (e.keyCode == KeyCode.W ? 1 : 0);
			sprint = (e.keyCode == KeyCode.LeftShift ? 2 : 1);

		}*/

		if (!boundsRect.Contains (GUIUtility.GUIToScreenPoint (e.mousePosition)) || x != 0 || z != 0) {
			
			float angle = Mathf.Atan2 ((mainData.reversed ? -1 : 1) * screenPoint.y, screenPoint.x) - Mathf.PI / 180.0f * editCenter.eulerAngles.y;
			Vector3 screenPoint3D = new Vector3 (Mathf.Cos (angle) + x, 0, Mathf.Sin (angle) + z);

			if (timer > -1)
				editCenter.position += mainData.cameraSpeedFactor * editCamSpeed * sprint * screenPoint3D * ((float)EditorApplication.timeSinceStartup - timer) * 2 * zoomAmount / 5.0f;
			
			timer = (float)EditorApplication.timeSinceStartup;

		} else
			timer = -1;

		if (!windowRect.Contains (e.mousePosition)) {

			mousePoint.exists = false;

			if (currentBlock)
				DestroyImmediate (currentBlock);
			
			if (BuildBox)
				DestroyImmediate (BuildBox);

			return;

		}

		float dist = 0.0f;
		Ray r = editCamera.ViewportPointToRay (screenPoint + new Vector2 (0.5f, 0.5f));
		Plane p = new Plane (new Vector3 (0, 1, 0), new Vector3 (0, height * mainData.AxisStep.y + mainData.OffStep.y, 0));

		if (p.Raycast (r, out dist)) {

			Vector3 target = r.origin + r.direction * dist + new Vector3 (mainData.OffStep.x, 0, mainData.OffStep.z);
			float newX = mainData.AxisStep.x * Mathf.Floor ((target.x + mainData.AxisStep.x / 2.0f) / mainData.AxisStep.x) - mainData.OffStep.x;
			float newY = height * mainData.AxisStep.y + mainData.OffStep.y;
			float newZ = mainData.AxisStep.z * Mathf.Floor ((target.z + mainData.AxisStep.z / 2.0f) / mainData.AxisStep.z) - mainData.OffStep.z;
			mousePoint.point = new Vector3 (newX, newY, newZ);

			mousePoint.exists = true;

			if (MouseClicked == false && currentBlock == null && mainData.currentBlock != null && mainData.currentBlock.prefab != null) {

				currentBlock = Instantiate (mainData.currentBlock.prefab, mousePoint.point, Quaternion.Euler (currentRot)) as GameObject;
				currentBlock.name = currentBlock.name.Substring (0, currentBlock.name.Length - 7);

			} else if (currentBlock != null)
				currentBlock.transform.position = mousePoint.point;

			if (BuildBox == null)
				BuildBox = EMBInterface.CreateBuildBox (mainData, mousePoint.point + (mainData.reversed ? -1 : 1) * mainData.AxisStep.y / 2 * Vector3.up);
			else {

				if (MouseClicked) {

					Vector3 worldPoint = EMBConversions.GridToCoordinates (mainData, startPoint);
					BuildBox.transform.position = (mousePoint.point + worldPoint) / 2 + (mainData.reversed ? -1 : 1) * mainData.AxisStep.y / 2 * Vector3.up;
					BuildBox.transform.localScale = new Vector3 (mainData.AxisStep.x + Mathf.Abs (worldPoint.x - mousePoint.point.x), mainData.AxisStep.y + Mathf.Abs (worldPoint.y - mousePoint.point.y), mainData.AxisStep.z + Mathf.Abs (worldPoint.z - mousePoint.point.z));

				} else {

					BuildBox.transform.position = mousePoint.point + (mainData.reversed ? -1 : 1) * mainData.AxisStep.y / 2 * Vector3.up;
					BuildBox.transform.localScale = mainData.AxisStep;

				}

			}

			if (MouseClicked) {
				
				IntVector3 point = EMBConversions.CoordinatesToGrid (mainData, mousePoint.point);
				GameObject startObject = currentlyDropped [startPoint];

				if (point != lastPoint) {

					lastPoint = point;

					IntVector3 minX;
					IntVector3 minY;
					IntVector3 minZ;

					if (point.x < startPoint.x)
						minX = point;
					else
						minX = startPoint;
					
					if (point.y < startPoint.y)
						minY = point;
					else
						minY = startPoint;
					
					if (point.z < startPoint.z)
						minZ = point;
					else
						minZ = startPoint;

					int i = Mathf.Abs (point.x - startPoint.x) + 1;
					int j = Mathf.Abs (point.y - startPoint.y) + 1;
					int k = Mathf.Abs (point.z - startPoint.z) + 1;

					foreach (IntVector3 v in currentlyDropped.Keys.ToArray ()) {

						if (v.x < minX.x || v.y < minY.y || v.z < minZ.z || v.x > Mathf.Max (point.x, startPoint.x) || v.y > Mathf.Max (point.y, startPoint.y) || v.z > Mathf.Max (point.z, startPoint.z)) {

							DestroyImmediate (currentlyDropped [v]);
							currentlyDropped.Remove (v);

						}

					}

					for (int a = 0; a < i; a++) {

						for (int b = 0; b < j; b++) {
							
							for (int c = 0; c < k; c++) {
								
								IntVector3 v = new IntVector3 (a + minX.x, b + minY.y, c + minZ.z);
								
								if (!currentlyDropped.ContainsKey (v)) {

									GameObject add = Instantiate (startObject, EMBConversions.GridToCoordinates (mainData, v), startObject.transform.rotation) as GameObject;
									add.name = add.name.Substring (0, add.name.Length - 7);
									currentlyDropped.Add (v, add);
								
								}
								
							}

						}

					}

				}

			}

			if (e.type == EventType.KeyDown) {
				
				switch (e.keyCode) {
					
				case KeyCode.UpArrow:

					currentRot.x = (currentRot.x + 360.0f / mainData.RotStep.x) % 360.0f;
					break;
					
				case KeyCode.DownArrow:
					
					currentRot.x = (currentRot.x - 360.0f / mainData.RotStep.x) % 360.0f;
					break;
					
				case KeyCode.LeftArrow:

					currentRot.z = (currentRot.z + 360.0f / mainData.RotStep.z) % 360.0f;
					break;
					
				case KeyCode.RightArrow:

					currentRot.z = (currentRot.z - 360.0f / mainData.RotStep.z) % 360.0f;
					break;
					
				case KeyCode.Escape:
					
					StopEdit ();
					break;
					
				default:
					break;
					
				}
				
				if (currentBlock)
					currentBlock.transform.eulerAngles = currentRot;
				
			}
			
			if (e.type == EventType.MouseDown) {

				if (e.button == 0 && currentBlock && mainData.currentBlock != null) {

					startPoint = EMBConversions.CoordinatesToGrid (mainData, mousePoint.point);
					lastPoint = startPoint;
					MouseClicked = true;
					currentlyDropped.Add (startPoint, currentBlock);
					currentBlock = null;
				
				} else if (e.button == 1) {
					
					currentRot.y = (currentRot.y + 360.0f / mainData.RotStep.y) % 360.0f;
					
					if (currentBlock)
						currentBlock.transform.eulerAngles = currentRot;

				}

			}
			
			if (e.type == EventType.MouseUp && MouseClicked == true) {

				if (BlueBuildBox != null)
					DestroyImmediate (BlueBuildBox);

				BlueBuildBox = BuildBox;
				BuildBox = null;

				BlueBuildBox.GetComponent<Renderer> ().material = EMBInterface.BlueBuildBoxMat;
				
				GameObject[] all = currentlyDropped.Values.ToArray ();

				PrefabDrop prefab = new PrefabDrop (all, BlueBuildBox.transform.position, BlueBuildBox.transform.localScale);
				
				MouseClicked = false;

				PrefabDrop.UndoList.Add (prefab);
				BBBIndex = PrefabDrop.UndoList.Count - 1;
				
				if (PrefabDrop.UndoList.Count > 100)
					PrefabDrop.UndoList.RemoveAt (0);

				for (int i = 0; i < all.Length; i++) {
					
					if (mainData.currentMapName != "") {
						
						Transform parent = GameObject.Find (mainData.currentMapName + "_" + mainData.currentBlock.parent).transform;
						
						if (parent != null)
							all [i].transform.parent = parent;
						
					} else
						all [i].transform.parent = null;

				}

				currentlyDropped = new Dictionary<IntVector3, GameObject> ();
				
			}

			if ((e.type == EventType.ScrollWheel)) {
				
				int delta = -Mathf.FloorToInt(e.delta.y)/3;
				
				editCenter.Translate (delta * mainData.AxisStep.y * Vector3.up);

				if (HelpPlane != null)
					HelpPlane.transform.position += delta * mainData.AxisStep.y * Vector3.up;

				height += delta;
				
			}

		} else if (currentBlock)
			DestroyImmediate (currentBlock);

	}

	void OnDestroy () {

		if (PrefabListWindow != null)
			PrefabListWindow.Close ();

		StopEdit ();
		
		if (editCenter != null) {

			s_editCenter.root = null;
			s_editCamera.root = null;
			DestroyImmediate (editCenter.gameObject);

		}

	}
	
	public void Initialize () {

		mainData = Data.data;
		
		GameObject g = new GameObject ();
		editCenter = g.transform;
		editCenter.name = "EMBCenter";
		
		g = new GameObject ();
		editCamera = g.AddComponent<Camera> ();
		g.name = "EMBCamera";
		editCamera.nearClipPlane = 0.01f;
		editCamera.farClipPlane = Mathf.Max (mainData.AxisStep.x, mainData.AxisStep.y, mainData.AxisStep.z) * mainData.camCullDistance * zoomAmount / 5.0f;
		editCamera.enabled = false;
		editCamera.transform.SetParent (editCenter);
		
		Vector3 pos = Vector3.zero;
		Vector3 rot = Vector3.zero;
		
		SceneView view = SceneView.lastActiveSceneView;
		
		if (view != null) {

			rot.y = Mathf.Round (SceneView.lastActiveSceneView.rotation.eulerAngles.y / 90.0f) * 90.0f;
			
			Camera cam = view.camera;
			
			if (cam != null)
				pos = cam.transform.position;
			
		}

		editCenter.position = pos;
		editCenter.rotation = Quaternion.Euler (rot);

		height = Mathf.FloorToInt ((pos.y - mainData.OffStep.y) / mainData.AxisStep.y);
		
		PlaceCenter (true);
		PlaceCamera (true);

		s_editCenter = new SmoothTransform (editCenter);
		s_editCamera = new SmoothTransform (editCamera.transform);
		
	}

	private void PlaceCenter (bool immediately = false) {

		Vector3 pos = editCenter.position;

		pos.y = height * mainData.AxisStep.y + mainData.OffStep.y;

		if (immediately)
			editCenter.position = pos;
		else
			s_editCenter.position = pos;

	}
	
	private void PlaceCamera (bool immediately = false) {

		float angle = Mathf.PI * mainData.camPerspective / 180.0f;
		float dist = Mathf.Max (mainData.AxisStep.x, mainData.AxisStep.y, mainData.AxisStep.z) * 10;
		float camHeight = (mainData.reversed ? -1 : 1) * Mathf.Sin (angle) * dist;
		float camLength = dist * Mathf.Cos (angle);
		Vector3 rot = new Vector3 ((mainData.reversed ? -1 : 1) * mainData.camPerspective, 0, 0);
		
		if (immediately) {
			
			editCamera.transform.localPosition = new Vector3 (0, camHeight, -camLength) * zoomAmount / 5.0f;
			editCamera.transform.localRotation = Quaternion.Euler (rot);
			
		} else {
			
			s_editCamera.position = new Vector3 (0, camHeight, -camLength) * zoomAmount / 5.0f;
			s_editCamera.rotation = Quaternion.Euler (rot);
			
		}
		
		editCamSpeed = Mathf.Max (mainData.AxisStep.x, mainData.AxisStep.z);
		
	}

	private void StopEdit () {

		if (currentBlock)
			DestroyImmediate (currentBlock);

		if (HelpPlane != null)
			DestroyImmediate (HelpPlane);
		
		if (BuildBox != null)
			DestroyImmediate (BuildBox);
		
		if (BlueBuildBox != null)
			DestroyImmediate (BlueBuildBox);

		if (mainData != null && mainData.enableAutoSave)
			EditorSceneManager.SaveOpenScenes ();

		currentColor = new Color (0.17f, 0.2f, 0.17f, 1.0f);
		backgroundColor = null;

		editing = false;

	}

	private void StartEdit () {
		
		if (PrefabListWindow != null)
			PrefabListWindow.Close ();

		height = Mathf.FloorToInt ((editCenter.position.y - mainData.OffStep.y) / mainData.AxisStep.y);
		
		currentColor = new Color (0.08f, 0.1f, 0.08f, 1.0f);
		backgroundColor = null;

		editing = true;

		HelpPlane = EMBInterface.CreateHelpPlane (mainData, height * mainData.AxisStep.y);
		PlaceCenter ();
		PlaceCamera ();

		EMBInterface.CreateHierarchy (mainData);

		int t = 0;
		int co = PrefabDrop.UndoList.Count;

		for (int i = 0; i < co; i++) {

			bool empty = true;

			foreach (GameObject g in PrefabDrop.UndoList [i-t].prefabs) {

				if (g != null) {

					empty = false;
					break;
					
				}
				
			}

			if (empty) {

				PrefabDrop.UndoList.RemoveAt (i-t);
				t += 1;

			}

		}

		if (PrefabDrop.UndoList.Count > 0) {

			BBBIndex = Mathf.Clamp (BBBIndex, 0, PrefabDrop.UndoList.Count - 1);
			PrefabDrop drop = PrefabDrop.UndoList [BBBIndex];

			BlueBuildBox = EMBInterface.CreateBuildBox (mainData, Vector3.zero);
			BlueBuildBox.transform.position = drop.blueBoxPosition;
			BlueBuildBox.GetComponent<Renderer> ().material = EMBInterface.BlueBuildBoxMat;
			BlueBuildBox.transform.localScale = drop.blueBoxScale;

		}

	}

	private void RandomizeLastPosition (bool x, bool y, bool z) {

		if (!selectionExists)
			return;

		foreach (GameObject g in PrefabDrop.UndoList [BBBIndex].prefabs) {

			Vector3 gridCenter = EMBConversions.CenterCoordinates (mainData, g.transform.position);
			Vector3 pos = g.transform.position;

			if (x)
				pos.x = gridCenter.x + ((Random.value - 0.5f) * mainData.AxisStep.x);

			if (y)
				pos.y = gridCenter.y + (Random.value * mainData.AxisStep.y);

			if (z)
				pos.z = gridCenter.z + ((Random.value - 0.5f) * mainData.AxisStep.z);

			g.transform.position = pos;

		}

	}
	
	private void ResetLastPosition (bool x, bool y, bool z) {
		
		if (!selectionExists)
			return;
		
		foreach (GameObject g in PrefabDrop.UndoList [BBBIndex].prefabs) {
			
			Vector3 gridCenter = EMBConversions.CenterCoordinates (mainData, g.transform.position);
			Vector3 pos = g.transform.position;
			
			if (x)
				pos.x = gridCenter.x;
			
			if (y)
				pos.y = gridCenter.y;
			
			if (z)
				pos.z = gridCenter.z;
			
			g.transform.position = pos;
			
		}
		
	}
	
	private void RandomizeLastRotation (bool x, bool y, bool z) {
		
		if (!selectionExists)
			return;
		
		foreach (GameObject g in PrefabDrop.UndoList [BBBIndex].prefabs) {

			Vector3 rot = g.transform.eulerAngles;
			
			if (x)
				rot.x = Random.value * 360;
			
			if (y)
				rot.y = Random.value * 360;
			
			if (z)
				rot.z = Random.value * 360;
			
			g.transform.eulerAngles = rot;
			
		}
		
	}
	
	private void ResetLastRotation (bool x, bool y, bool z) {
		
		if (!selectionExists)
			return;
		
		foreach (GameObject g in PrefabDrop.UndoList [BBBIndex].prefabs) {
			
			Vector3 rot = g.transform.eulerAngles;
			
			if (x)
				rot.x = 0.0f;
			
			if (y)
				rot.y = 0.0f;
			
			if (z)
				rot.z = 0.0f;
			
			g.transform.eulerAngles = rot;
			
		}
		
	}

	private void NextPrefabDrop () {

		if (!selectionExists || BBBIndex == PrefabDrop.UndoList.Count - 1)
			return;

		BBBIndex += 1;
		PrefabDrop d = PrefabDrop.UndoList [BBBIndex];

		BlueBuildBox.transform.position = d.blueBoxPosition;
		BlueBuildBox.transform.localScale = d.blueBoxScale;

	}
	
	private void PreviousPrefabDrop () {
		
		if (!selectionExists || BBBIndex == 0)
			return;
		
		BBBIndex -= 1;
		PrefabDrop d = PrefabDrop.UndoList [BBBIndex];
		
		BlueBuildBox.transform.position = d.blueBoxPosition;
		BlueBuildBox.transform.localScale = d.blueBoxScale;
		
	}

	private void ClampCollision (bool withNormals) {

		if (!selectionExists)
			return;

		if (BlueBuildBox)
			BlueBuildBox.SetActive (false);

		if (BuildBox)
			BuildBox.SetActive (false);

		if (HelpPlane)
			HelpPlane.SetActive (false);

		PrefabDrop d = PrefabDrop.UndoList [BBBIndex];

		foreach (GameObject g in d.prefabs) {

			RaycastHit hit;
			Collider[] cols = g.GetComponentsInChildren<Collider> ();

			foreach (Collider c in cols)
				c.enabled = false;

			if (Physics.Raycast (g.transform.position, Vector3.down, out hit)) {

				Collider col = null;
				float y = Mathf.Infinity;

				foreach (Collider c in cols) {

					if (c.transform.position.y - c.bounds.size.y / 2 < y) {

						y = c.transform.position.y - c.bounds.size.y / 2;
						col = c;

					}

				}
				
				if (withNormals) {
					
					float alpha = Mathf.Atan2 (hit.normal.y, hit.normal.x) * 180.0f / Mathf.PI - 90;
					float beta = Mathf.Atan2 (hit.normal.y, hit.normal.z) * 180.0f / Mathf.PI - 90;
					
					g.transform.eulerAngles = new Vector3 (beta, currentRot.y, alpha);
					
				}
				
				foreach (Collider c in cols)
					c.enabled = true;

				if (col == null) {

					g.transform.position = hit.point + 0.002f * Vector3.up;

				} else {

					float diff = col.bounds.center.y - g.transform.position.y;
					g.transform.position = hit.point + (col.bounds.size.y / 2 + 0.002f - diff) * Vector3.up;

				}

			} else {
				
				foreach (Collider c in cols)
					c.enabled = true;

			}

		}

		ReshapeBlueBuildBox (d);
		
		if (BlueBuildBox) {

			BlueBuildBox.SetActive (true);
			BlueBuildBox.transform.position = d.blueBoxPosition;
			BlueBuildBox.transform.localScale = d.blueBoxScale;

		}
		
		if (BuildBox)
			BuildBox.SetActive (true);
		
		if (HelpPlane)
			HelpPlane.SetActive (true);

	}
	
	private void ReshapeBlueBuildBox (PrefabDrop drop) {

		Vector3 max = new Vector3 (Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
		Vector3 min = new Vector3 (Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

		foreach (GameObject g in drop.prefabs) {

			Vector3 gridPoint = EMBConversions.CenterCoordinates (mainData, g.transform.position);

			if (gridPoint.x < min.x)
				min.x = gridPoint.x;
			
			if (gridPoint.y < min.y)
				min.y = gridPoint.y;
			
			if (gridPoint.z < min.z)
				min.z = gridPoint.z;

			if (gridPoint.x > max.x)
				max.x = gridPoint.x;
			
			if (gridPoint.y > max.y)
				max.y = gridPoint.y;
			
			if (gridPoint.z > max.z)
				max.z = gridPoint.z;

		}

		drop.blueBoxPosition = (min + max) / 2 + Vector3.up * mainData.AxisStep.y / 2;

		Vector3 scale = new Vector3 ();

		scale.x = max.x - min.x + mainData.AxisStep.x;
		scale.y = max.y - min.y + mainData.AxisStep.y;
		scale.z = max.z - min.z + mainData.AxisStep.z;
		drop.blueBoxScale = scale;
		
	}

}

public class EasyMapBuilderPrefabList : EditorWindow {

	private Data.MainData mainData;

	private Texture2D b_BackgroundColor;
	private Texture2D backgroundColor {
		
		get {
			
			if (b_BackgroundColor == null) {
				
				b_BackgroundColor = new Texture2D (1, 1);
				b_BackgroundColor.SetPixel (1, 1, new Color (0.17f, 0.2f, 0.17f, 1.0f));
				b_BackgroundColor.Apply ();
				
			}
			
			return b_BackgroundColor;
			
		}
		
	}

	private Vector2 EditItemsScroll;
	
	private Object ImportFolder;

	private string search = "";

	public static void  ShowWindow () {
		
		EasyMapBuilder.PrefabListWindow = EditorWindow.GetWindow(typeof(EasyMapBuilderPrefabList));
		
	}
	
	void OnGUI () {

		if (mainData == null)
			mainData = Data.data;

		Color normalColor = GUI.color;

		Data.MainData tempData = new Data.MainData (mainData);

		GUI.DrawTexture (new Rect (0, 0, Screen.width, Screen.height), backgroundColor, ScaleMode.StretchToFill);

		GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);

		GUILayout.BeginVertical ("box");

		GUILayout.Label ("Import Folder(s)");

		EditorGUILayout.Space ();

		ImportFolder = EditorGUILayout.ObjectField ("Folder : ", ImportFolder, typeof(Object), false);

		EditorGUILayout.Space ();
		
		GUI.color = new Color (0.6f, 1.0f, 0.5f, 1.0f);
		
		if (GUILayout.Button ("Import", GUILayout.Height (30)))
			Import.ImportCurrentFolder (ImportFolder);
		
		GUI.color = new Color (0.8f, 1.0f, 0.8f, 1.0f);
		
		GUILayout.EndVertical ();
		
		EditorGUILayout.Space ();

		GUI.color = normalColor;

		EditItemsScroll = EMBInterface.DrawPrefabList (mainData, Screen.width, EditItemsScroll, ref search);
		
		if (!mainData.Compare(tempData))
			Data.SaveData ();
		
	}

}