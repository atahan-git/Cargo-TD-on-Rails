using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapController : MonoBehaviour {
	public static MapController s;

	private void Awake() {
		s = this;
	}

	private void Start() {
		trainTargetPos = train.transform.position;
	}

	[Serializable]
	public class MapState {
		public List<SegmentState> leftSegments = new List<SegmentState>();
		public List<SegmentState> rightSegments = new List<SegmentState>();
		public SegmentState bossSegment;
		public int pitStopDepth = 4;
		public int eliteDepth = 5;
		public int bossDepth = 7;
	}

	[Serializable]
	public class SegmentState {
		public float length;
		public UpgradesController.PathEnemyType enemyType;
	}

	public Transform trackPiecesParent;
	public GameObject emptyTrack;
	public GameObject goodThingTrack;
	public GameObject eliteTrack;
	public GameObject bossTrack;
	public GameObject switchPiece;
	public GameObject switchWithGemPiece;

	public GameObject train;
	public Vector3 trainTargetPos;

	public MapState currentMap;
	public void MakeNewMap() {
	    var newMap = new MapState();
	    
	    int bossDepth = 7;
	    int easyDepth = 2;
	    int pitStopMiddleDepth = 4;
	    int eliteDepth = 5;
	    float emptyChance = 0.25f;

	    for (int i = 0; i < bossDepth; i++) {
		    var length = PlayStateMaster.s.currentLevel.GetRandomSegmentLength()*1.5f;
		    if (i == pitStopMiddleDepth) {
			    length /= 1.5f;
			    newMap.leftSegments.Add(new SegmentState() {
				    enemyType = UpgradesController.s.GetPitStopEnemy(),
				    length = length
			    });
			    newMap.rightSegments.Add(new SegmentState() {
				    enemyType = UpgradesController.s.GetPitStopEnemy(),
				    length = length
			    });
		    } else {

			    var emptyRoll = Random.value < emptyChance;
			    emptyRoll = false;
			    
			    UpgradesController.PathEnemyType enemy;
			    if (i <= easyDepth) {
				    enemy = UpgradesController.s.GetEasyEnemy();
				    length /= 2f;
			    }else if (i == eliteDepth) {
				    enemy = UpgradesController.s.GetEliteEnemy();
				    length *= 1.2f;
				    emptyRoll = false;
			    } else {
				    enemy = UpgradesController.s.GetPathEnemy();
			    }

			    var leftEnemy = enemy;
			    var rightEnemy = enemy;

			    if (emptyRoll) {
				    if (Random.value < 0.5f) {
					    leftEnemy = UpgradesController.s.GetEmptyEnemy();
				    } else {
					    rightEnemy = UpgradesController.s.GetEmptyEnemy();
				    }
			    }
			    
			    newMap.leftSegments.Add(new SegmentState() {
				    enemyType = leftEnemy,
				    length = length
			    });
			    newMap.rightSegments.Add(new SegmentState() {
				    enemyType = rightEnemy,
				    length = length
			    });
		    }
	    }

	    newMap.bossSegment = new SegmentState() {
		    enemyType = UpgradesController.s.GetBossEnemy(),
		    length = PlayStateMaster.s.currentLevel.GetRandomSegmentLength()
	    };

	    newMap.bossDepth = bossDepth;
	    newMap.pitStopDepth = pitStopMiddleDepth;
	    newMap.eliteDepth = eliteDepth;

	    currentMap = newMap;
	    MakeMinimap();
    }

    void MakeMinimap() {
	    trackPiecesParent.DeleteAllChildren();

	    var minimapLengthMultiplier = 0.25f;

	    for (int i = 0; i < currentMap.bossDepth; i++) {
		    if (i == currentMap.pitStopDepth) {
			    Instantiate(goodThingTrack, trackPiecesParent).GetComponent<MiniGUI_TrackPath>().SetUpTrack(currentMap.leftSegments[i].length * minimapLengthMultiplier);
		    } else if (i == currentMap.eliteDepth) {
			    Instantiate(eliteTrack, trackPiecesParent).GetComponent<MiniGUI_TrackPath>().SetUpTrack(currentMap.leftSegments[i].length * minimapLengthMultiplier);
		    } else {
			    Instantiate(emptyTrack, trackPiecesParent).GetComponent<MiniGUI_TrackPath>().SetUpTrack(currentMap.leftSegments[i].length * minimapLengthMultiplier);
		    }

		    if (i % 2 == 1) {
			    Instantiate(switchWithGemPiece, trackPiecesParent);
		    } else {
			    Instantiate(switchPiece, trackPiecesParent);
		    }
	    }

	    Instantiate(bossTrack, trackPiecesParent).GetComponent<MiniGUI_TrackPath>().SetUpTrack(currentMap.bossSegment.length*minimapLengthMultiplier);

	    trackPiecesParent.GetComponent<ManualHorizontalLayoutGroup>().isDirty = true;
    }


    public void UpdateTrainPosition() {
	    var trainDepth = PathAndTerrainGenerator.s.currentPathTree.myDepth;

	    var targetTrack = trackPiecesParent.GetChild(Mathf.Min(trainDepth*2, trackPiecesParent.childCount-1)).GetComponent<RectTransform>();
	    
	    trainTargetPos = targetTrack.TransformPoint(targetTrack.rect.center);
	    
	    if (trainDepth == currentMap.pitStopDepth || trainDepth == currentMap.bossDepth) {
		    trainTargetPos += Vector3.left * 0.30f;
	    }
    }

    private void Update() {
	    UpdateTrainPosition();
	    train.transform.position = Vector3.Lerp(train.transform.position, trainTargetPos, 1.5f*Time.deltaTime);
    }

    public UpgradesController.PathEnemyType GetMapEnemyType( bool isLeft, int depth) {
	    
	    if (depth >= currentMap.bossDepth) {
		    return currentMap.bossSegment.enemyType;
	    }
	    
	    if (isLeft) {
		    return currentMap.leftSegments[depth].enemyType;
	    } else {
		    return currentMap.rightSegments[depth].enemyType;
	    }
    }

    public float GetSegmentDistance(bool isLeft, int depth) {
	    
	    if (depth >= currentMap.bossDepth) {
		    return currentMap.bossSegment.length;
	    }
	    
	    if (isLeft) {
		    return currentMap.leftSegments[depth].length;
	    } else {
		    return currentMap.rightSegments[depth].length;
	    }
    }
}
