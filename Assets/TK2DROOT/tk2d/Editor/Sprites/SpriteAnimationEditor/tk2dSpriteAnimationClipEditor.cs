using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace tk2dEditor.SpriteAnimationEditor
{
	public class ClipEditor
	{
		// Accessors
		tk2dSpriteAnimationClip clip = null;
		public tk2dSpriteAnimationClip Clip
		{
			get { return clip; }
			set { SetClip(value); }
		}

		public void InitForNewClip() { selectClipNameField = true; timelineEditor.CurrentState.selectedFrame = 0; }
		bool selectClipNameField = false;

		// Locals
		int minInspectorWidth = 170;
		int inspectorWidth { get { return tk2dPreferences.inst.animInspectorWidth; } set { tk2dPreferences.inst.animInspectorWidth = Mathf.Max(value, minInspectorWidth); } }

		tk2dSpriteAnimationPreview preview = null;
		TimelineEditor timelineEditor = new TimelineEditor();

		// Events
		public delegate void ClipEventDelegate(tk2dSpriteAnimationClip clip, int data);
		public event ClipEventDelegate clipNameChangedEvent;
		public event ClipEventDelegate clipDeletedEvent;
		public event ClipEventDelegate clipSelectionChangedEvent;
		void OnClipNameChanged() { if (clipNameChangedEvent != null) clipNameChangedEvent(clip, 0); }
		void OnClipDeleted() { if (clipDeletedEvent != null) clipDeletedEvent(clip, 0); }
		bool OnClipSelectionChanged(int direction) { if (clipSelectionChangedEvent != null) clipSelectionChangedEvent(clip, direction); return clipSelectionChangedEvent != null; }
		void Repaint() { HandleUtility.Repaint(); }

		// Editor operations
		public tk2dEditor.SpriteAnimationEditor.AnimOperator[] animOps = new tk2dEditor.SpriteAnimationEditor.AnimOperator[0];

		// Construction/Destruction
		public void Destroy()
		{
			if (preview != null)
				preview.Destroy();
			if (sprite != null)
				ObjectPoolController.Destroy(sprite.gameObject);
				//Object.DestroyImmediate(sprite.gameObject);
		}

		// Frame groups
		public class FrameGroup
		{
			public tk2dSpriteCollectionData spriteCollection;
			public int spriteId;
			public List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
			public int startFrame = 0; // this is a cache value used during the draw loop

			public bool SetFrameCount(int targetFrameCount)
			{
				bool changed = false;
				if (frames.Count > targetFrameCount)
				{
					frames.RemoveRange(targetFrameCount, frames.Count - targetFrameCount);
					changed = true;
				}
				while (frames.Count < targetFrameCount)
				{
					tk2dSpriteAnimationFrame f = new tk2dSpriteAnimationFrame();
					f.spriteCollection = spriteCollection;
					f.spriteId = spriteId;
					frames.Add(f);
					changed = true;
				}
				return changed;
			}

			public List<tk2dSpriteAnimationFrame> DuplicateFrames(List<tk2dSpriteAnimationFrame> source)
			{
				List<tk2dSpriteAnimationFrame> dest = new List<tk2dSpriteAnimationFrame>();
				foreach (tk2dSpriteAnimationFrame f in source)
				{
					tk2dSpriteAnimationFrame q = new tk2dSpriteAnimationFrame();
					q.CopyFrom(f);
					dest.Add(q);
				}
				return dest;
			}
			
			public void Update()
			{
				foreach (tk2dSpriteAnimationFrame frame in frames)
				{
					frame.spriteCollection = spriteCollection;
					frame.spriteId = spriteId;
				}
			}
		}
		List<FrameGroup> frameGroups = new List<FrameGroup>();

		// Animated sprite
		public static tk2dAnimatedSprite.AnimationEventDelegate PreviewEventHandler = null;
		tk2dAnimatedSprite sprite = null;
		tk2dAnimatedSprite Sprite {
			get {
				if (sprite == null)
				{
					GameObject go = new GameObject("@AnimatedSprite");
					go.hideFlags = HideFlags.HideAndDontSave;
					#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9
						go.active = false;
					#else
						go.SetActive(false);
					#endif
					sprite = go.AddComponent<tk2dAnimatedSprite>();
					sprite.animationEventDelegate += PreviewEventHandler;
				}
				return sprite;
			}
		}

		// Internal set clip, reset all 
		void SetClip(tk2dSpriteAnimationClip clip)
		{
			if (this.clip != clip)
			{
				// reset stuff
				this.clip = clip;

				timelineEditor.Reset();

				if (!repeatPlayAnimation) playAnimation = false;
				this.Sprite.Stop();

				// build frame groups
				if (clip != null)
				{
					frameGroups.Clear();
					tk2dSpriteCollectionData lastSc = null;
					int lastSpriteId = -1;
					FrameGroup frameGroup = null;
					for (int i = 0; i < clip.frames.Length; ++i)
					{
						tk2dSpriteAnimationFrame f = clip.frames[i];
						if (f.spriteCollection != lastSc || f.spriteId != lastSpriteId)
						{
							if (frameGroup != null) frameGroups.Add(frameGroup);
							frameGroup = new FrameGroup();
							frameGroup.spriteCollection = f.spriteCollection;
							frameGroup.spriteId = f.spriteId;
						}
						lastSc = f.spriteCollection;
						lastSpriteId = f.spriteId;
						frameGroup.frames.Add(f);
					}
					if (frameGroup != null) frameGroups.Add(frameGroup);
				}
			}
		}

		double previousTimeStamp = -1.0f;
		float deltaTime = 0.0f;

		void DrawPreview()
		{
			GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			Rect r = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			// get frame from frame group
			if (playAnimation)
			{
				preview.Draw(r, Sprite.GetCurrentSpriteDef());
			}
			else
			{
				if (timelineEditor.CurrentState.selectedFrame == -1)
				{
					preview.Draw(r, null);
				}
				else
				{
					int frame = timelineEditor.CurrentState.selectedFrame;
					if (frameGroups != null && frame < frameGroups.Count)
					{
						FrameGroup fg = frameGroups[frame];
						tk2dSpriteCollectionData sc = fg.spriteCollection.inst;
						preview.Draw(r, sc.spriteDefinitions[fg.spriteId]);
					}
				}
			}

			GUILayout.EndVertical();
		}

		void DrawClipInspector()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Clip", EditorStyles.largeLabel);
			if (GUILayout.Button("Delete", GUILayout.Width(46)))
				OnClipDeleted();
			GUILayout.EndHorizontal();

			GUI.SetNextControlName("tk2dAnimName");
			string changedName = EditorGUILayout.TextField("Name", clip.name).Trim();
			if (selectClipNameField)
			{
				GUI.FocusControl("tk2dAnimName");
				selectClipNameField = false;
			}
			if (changedName != clip.name && changedName.Length > 0)
			{
				clip.name = changedName;
				OnClipNameChanged();
			}
			EditorGUILayout.IntField("Frames", clip.frames.Length);
			float fps = EditorGUILayout.FloatField("Frame rate", clip.fps);
			if (fps > 0) clip.fps = fps;
			float clipTime = clip.frames.Length / fps;
			float newClipTime = EditorGUILayout.FloatField("Clip time", clipTime);
			if (newClipTime > 0 && newClipTime != clipTime)
				clip.fps = clip.frames.Length / newClipTime;
			tk2dSpriteAnimationClip.WrapMode newWrapMode = (tk2dSpriteAnimationClip.WrapMode)EditorGUILayout.EnumPopup("Wrap Mode", clip.wrapMode);
			if (clip.wrapMode == tk2dSpriteAnimationClip.WrapMode.LoopSection)
			{
				clip.loopStart = EditorGUILayout.IntField("Loop Start", clip.loopStart);
				clip.loopStart = Mathf.Clamp(clip.loopStart, 0, clip.frames.Length - 1);
			}
			if (newWrapMode != clip.wrapMode)
			{
				if (newWrapMode == tk2dSpriteAnimationClip.WrapMode.Single && clip.frames.Length > 1)
				{
					// Will we be truncating the animation?
					if (EditorUtility.DisplayDialog("Wrap mode -> Single", "This will truncate your clip to a single frame. Do you want to continue?", "Yes", "No"))
					{
						clip.wrapMode = newWrapMode;
						frameGroups.RemoveRange(1, frameGroups.Count - 1);
						frameGroups[0].SetFrameCount(1);
						ClipEditor.RecalculateFrames(clip, frameGroups);
					}
				}
				else
				{
					clip.wrapMode = newWrapMode;
				}
			}

			// Tools
			GUILayout.Space(8);
			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(" ");
			GUILayout.BeginVertical();
			bool changed = false;
			foreach (tk2dEditor.SpriteAnimationEditor.AnimOperator animOp in animOps)
			{
				changed = animOp.OnClipInspectorGUI(clip, frameGroups, timelineEditor.CurrentState);
				if ((animOp.AnimEditOperations & tk2dEditor.SpriteAnimationEditor.AnimEditOperations.ClipContentChanged) != tk2dEditor.SpriteAnimationEditor.AnimEditOperations.None)
				{
				 	RecalculateFrames();
				 	changed = true;
				}
				if ((animOp.AnimEditOperations & tk2dEditor.SpriteAnimationEditor.AnimEditOperations.ClipNameChanged) != tk2dEditor.SpriteAnimationEditor.AnimEditOperations.None)
				{
					OnClipNameChanged();
					changed = true;
				}
			}
			if (changed) Repaint();
			GUILayout.EndVertical();			
			GUILayout.EndHorizontal();

			GUILayout.Space(8);
		}

		void DrawFrameInspector()
		{
			GUILayout.Label("Frame", EditorStyles.largeLabel, GUILayout.ExpandWidth(true));

			FrameGroup fg = frameGroups[timelineEditor.CurrentState.selectedFrame];
			bool spriteChanged = false;

			tk2dSpriteCollectionData newCollection = tk2dSpriteGuiUtility.SpriteCollectionPopup("Collection", fg.spriteCollection, true, fg.spriteId);
			if (newCollection != fg.spriteCollection)
			{
				fg.spriteCollection = newCollection;
				if (fg.spriteId < 0 || fg.spriteId >= fg.spriteCollection.Count 
					|| !fg.spriteCollection.inst.spriteDefinitions[fg.spriteId].Valid)
					fg.spriteId = fg.spriteCollection.FirstValidDefinitionIndex;
				spriteChanged = true;
			}

			if (fg.spriteCollection != null)
			{
				int spriteId = tk2dSpriteGuiUtility.SpriteSelectorPopup("Sprite", fg.spriteId, fg.spriteCollection);
				if (spriteId != fg.spriteId)
				{
					fg.spriteId = spriteId;
					spriteChanged = true;
				}
			}

			int numFrames = EditorGUILayout.IntField("Frames", fg.frames.Count);
			if (numFrames != fg.frames.Count && numFrames > 0)
			{
				if (fg.SetFrameCount(numFrames))
				{
					RecalculateFrames();
					Repaint();
				}
			}

			float time0 = fg.frames.Count / clip.fps;
			float time = EditorGUILayout.FloatField("Time", time0);
			if (time != time0)
			{
				int frameCount = Mathf.Max(1, (int)Mathf.Ceil(time * clip.fps));
				if (fg.SetFrameCount(frameCount))
				{
					RecalculateFrames();
					Repaint();
				}
			}

			if (spriteChanged)
			{
				foreach (tk2dSpriteAnimationFrame frame in fg.frames)
				{
					frame.spriteCollection = fg.spriteCollection;
					frame.spriteId = fg.spriteId;
					RecalculateFrames();
				}
			}

			// Tools
			GUILayout.Space(8);
			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(" ");
			GUILayout.BeginVertical();
			bool changed = false;
			foreach (tk2dEditor.SpriteAnimationEditor.AnimOperator animOp in animOps)
			{
				changed = animOp.OnFrameGroupInspectorGUI(clip, frameGroups, timelineEditor.CurrentState);
				if ((animOp.AnimEditOperations & tk2dEditor.SpriteAnimationEditor.AnimEditOperations.ClipContentChanged) != tk2dEditor.SpriteAnimationEditor.AnimEditOperations.None)
				{
					RecalculateFrames();
					changed = true;
				}
			}
			if (changed) Repaint();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		bool repeatPlayAnimation = false;
		const float repeatPlayWaitTimeConst = 1.0f;
		float repeatPlayWaitTime = 0;
		bool playAnimation = false;

		void TogglePlayAnimation()
		{
			if (playAnimation)
			{
				if (Sprite != null) Sprite.Stop();
				playAnimation = false;
			}
			else
			{
				PlayAnimation();
			}
		}

		void PlayAnimation()
		{
			if (Clip != null && Clip.frames.Length > 0)
			{
				playAnimation = true;
				previousTimeStamp = EditorApplication.timeSinceStartup; // reset time to avoid huge delta time
				repeatPlayWaitTime = repeatPlayWaitTimeConst;
				Sprite.Play(Clip, 0);
				Repaint();
			}
		}

		void DrawTransportToolbar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

			if (Event.current.type == EventType.Repaint)
			{
				if (playAnimation && !Sprite.Playing)
				{
					if (repeatPlayAnimation || clip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Single)
					{
						if (repeatPlayWaitTime > 0.0f)
						{
							repeatPlayWaitTime -= deltaTime;
							Repaint();
						}
						else
						{
							Sprite.Play(Clip, 0);
							repeatPlayWaitTime = repeatPlayWaitTimeConst;
							Repaint();
						}
					} 
					else 
					{
						playAnimation = false;
					}
				}
			}

			GUIContent stopContent = new GUIContent("Stop", "Stop playing the current animation (Enter)");
			GUIContent startContent = new GUIContent("Play", "Start playing the current animation (Enter)");
			GUIContent playLabel = playAnimation ? stopContent : startContent;
			bool newPlayAnimation = GUILayout.Toggle(playAnimation, playLabel, EditorStyles.toolbarButton, GUILayout.Width(35));
			if (newPlayAnimation != playAnimation)
			{
				if (newPlayAnimation == true)
				{
					PlayAnimation();
				}
				else
				{
					Sprite.Stop();
					Repaint();
				}
				playAnimation = newPlayAnimation;
			}
			
			GUI.enabled = clip == null || (clip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Once);
			repeatPlayAnimation = GUILayout.Toggle(repeatPlayAnimation, "Repeat", EditorStyles.toolbarButton);
			GUI.enabled = true;
			
			GUILayout.FlexibleSpace();

			preview.GridType = (tk2dSpriteAnimationPreview.GridTypes)EditorGUILayout.EnumPopup(preview.GridType, EditorStyles.toolbarDropDown, GUILayout.Width(95));
			GUILayout.EndHorizontal();
		}

		void DrawTriggerInspector()
		{
			GUILayout.Label("Trigger", EditorStyles.largeLabel, GUILayout.ExpandWidth(true));
			
			tk2dSpriteAnimationFrame frame = clip.frames[timelineEditor.CurrentState.selectedTrigger];
			EditorGUILayout.LabelField("Frame", timelineEditor.CurrentState.selectedTrigger.ToString());

			frame.eventInfo = EditorGUILayout.TextField("Info", frame.eventInfo);
			frame.eventFloat = EditorGUILayout.FloatField("Float", frame.eventFloat);
			frame.eventInt = EditorGUILayout.IntField("Int", frame.eventInt);

			GUILayout.Space(8);
			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(" ");
			GUILayout.BeginVertical();

			if (GUILayout.Button("Delete", EditorStyles.miniButton))
			{
				clip.frames[timelineEditor.CurrentState.selectedTrigger].ClearTrigger();
				timelineEditor.CurrentState.Reset();
				Repaint();
			}

			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		Vector2 inspectorScrollbar = Vector2.zero;
		void DrawInspector()
		{
			EditorGUIUtility.LookLikeControls(80.0f, 40.0f);
			
			inspectorScrollbar = GUILayout.BeginScrollView(inspectorScrollbar, GUILayout.ExpandHeight(true), GUILayout.Width(inspectorWidth));

			// Heading
			GUILayout.BeginVertical(tk2dEditorSkin.SC_InspectorHeaderBG, GUILayout.ExpandWidth(true));
			DrawClipInspector();
			GUILayout.EndVertical();

			DrawTransportToolbar();
			
			// Contents
			GUILayout.BeginVertical(tk2dEditorSkin.SC_InspectorBG, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			if (timelineEditor.CurrentState.selectedFrame != -1) DrawFrameInspector();
			if (timelineEditor.CurrentState.selectedTrigger != -1) DrawTriggerInspector();
			GUILayout.EndVertical();

			GUILayout.EndScrollView();

			Rect viewRect = GUILayoutUtility.GetLastRect();

			// Resize handle
			inspectorWidth -= (int)tk2dGuiUtility.DragableHandle(4819518, viewRect, 0, tk2dGuiUtility.DragDirection.Horizontal);
		}

		public static void RecalculateFrames(tk2dSpriteAnimationClip clip, List<FrameGroup> frameGroups)
		{
			List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
			foreach (var v in frameGroups)
				frames.AddRange(v.frames);
			clip.frames = frames.ToArray();
		}
		// Internal convenience function
		void RecalculateFrames()
		{
			ClipEditor.RecalculateFrames(clip, frameGroups);
		}

		void HandleKeyboardShortcuts()
		{
			Event ev = Event.current;

			if (ev.type == EventType.KeyDown && GUIUtility.keyboardControl == 0)
			{
				switch (ev.keyCode)
				{
					case KeyCode.UpArrow: if (OnClipSelectionChanged(-1)) ev.Use(); break;
					case KeyCode.DownArrow: if (OnClipSelectionChanged(1)) ev.Use(); break;
					case KeyCode.Return: TogglePlayAnimation(); ev.Use(); break;
					case KeyCode.F: if (preview != null) preview.ResetTransform(); ev.Use(); break;
				}
			}
		}

		public void Draw(int windowWidth)
		{
			if (clip == null)
				return;

			if (preview == null)
				preview = new tk2dSpriteAnimationPreview();

			// Update
			if (Event.current.type == EventType.Repaint)
			{
				double t = EditorApplication.timeSinceStartup;
				if (previousTimeStamp < 0) previousTimeStamp = t;
				deltaTime = (float)(t - previousTimeStamp);
				previousTimeStamp = t;

				// Update sprite
				if (Sprite.Playing)
				{
					Sprite.ClipFps = clip.fps;
					Sprite.UpdateAnimation(deltaTime);
					Repaint(); // refresh
				}
			}

			// Idle key handling
			if (GUIUtility.keyboardControl == 0)
				HandleKeyboardShortcuts();

			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			DrawPreview();
			DrawInspector();
			GUILayout.EndHorizontal();

			float clipTimeMarker = -1.0f;
			if (playAnimation)
			{
				float clipTime = Sprite.Playing ? Sprite.EditorClipTime : 0.0f;
				clipTimeMarker = clipTime;
			}

			timelineEditor.Draw(windowWidth, clip, frameGroups, clipTimeMarker);
			GUILayout.EndVertical();
		}
	}
}