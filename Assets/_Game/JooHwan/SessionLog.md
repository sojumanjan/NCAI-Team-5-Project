# Session Log — JooHwan (테트리스 미니게임)

이 문서는 Claude Code(UnityMCP 연동)와의 작업 세션에서 있었던 논의/결정/구현 내용을 정리한 기록입니다.

## 1. 세부 기획 논의

md 기획서(`미니게임파티_기획.md`)를 기반으로 3개 미니게임(테트리스/팩맨/핀볼)의 메카닉 상세 규칙을 논의하고 `미니게임파티_세부기획.md`(데스크톱)로 정리했습니다. 핵심 결정 사항:

- **선택 구조**: 허브 내 별도 미니게임 선택 메뉴에서 3개 중 자유 선택 (순차 진행 아님), 게임마다 시작/설명 버튼 배치, 클리어 시 표시
- **시점 원칙**: 3개 게임 모두 **1인칭 고정** 기본 시점으로 통일 (원래는 테트리스/핀볼을 3인칭으로 논의했으나 최종적으로 1인칭 전환 결정)
  - 이유: 캐릭터가 Humanoid 리깅이 아닐 가능성이 있어, 특수 동작(매달리기 등)을 리깅 없이 스크립트/Tween으로 구현하기 쉬운 1인칭이 유리
  - 1인칭 전환에 따라 Skybox 노출 문제가 생겨, 테트리스/핀볼 모두 **사방이 막힌 밀폐 공간**으로 설계 (게임기 내부 컨셉과도 부합)
- **테트리스**: 7레인 고정 그리드, 디자이너가 짠 고정 낙하 시퀀스(랜덤 배제), 관전(전략) 카메라 스위칭 + 다음 블록 프리뷰 UI, 끼임 판정 사망, 특정 높이 도달 시 클리어
- **팩맨**: 1인칭, 파워펠릿 최대 2개 소지, 고스트 5마리(NavMesh 순찰+추격), 발광 타이밍에만 처치 가능, 리깅 없이 Emission 셰이더로 상태 표현
- **핀볼**: 1인칭, 범퍼만 사용(플리퍼 삭제), 코인 전체 수집 후 EXIT, 목숨 3개 + 낙사 리스폰

## 2. Unity 작업 — 테트리스 씬

### 씬/아레나
- `JooHwan_MiniGame_Tetris` 씬 생성 (`Assets/_Game/JooHwan/03. Scenes/`)
- 팀 공동 UI 규격 적용: Canvas Scaler `Scale With Screen Size`, Reference 1920x1080, Match 0.5
- 아레나 크기: 블록 크기 3 기준, 가로 21(7레인×3), 높이 45(15단×3), 깊이 3(블록 Z축 기준 꽉 맞춤, 여유 없음)
- 벽 4면 + 바닥으로 밀폐 공간 구성

### Input Actions
- `JooHwan_InputActions.inputactions`: JooHwan 담당 3개 미니게임 전체가 공유하는 단일 애셋
- 액션맵을 게임별로 나누지 않고 **하나의 Player 맵**으로 통합 (스크립트도 공용으로 쓸 계획이라 나눌 실익이 없다고 판단)
- 액션: Move(WASD), Look(마우스 Delta), Jump(Space), CameraSwitch(Tab), Throw(마우스 좌클릭), Pause(ESC)

### 플레이어 스크립트
- **`PlayerController.cs`**: 이동/시점 담당. CharacterController 기반. `jumpEnabled` 옵션으로 팩맨(점프 없음) 등 다른 게임에서도 재사용 가능하도록 설계. Move speed / Jump height / Mouse sensitivity 모두 Inspector 노출
- **`CameraRig.cs`**: 1인칭 카메라 ↔ 관전(오버뷰) 카메라 전환 전담. `ToggleCamera()`로 토글, 전환 시 PlayerController 비활성화(입력 차단), 시야를 가리는 벽(Wall_Back)의 MeshRenderer 토글(콜라이더는 유지해 안전), 관전 전용 UI(`OverviewUI`) 표시/숨김까지 한 번에 처리
- **`ClimbController.cs`**: 손 Sphere 2개로 매달려 오르는 등반 연출. Raycast 3발(낮은 높이 막힘 + 높은 높이 뚫림 + 정확한 턱 높이)로 등반 가능 여부 판정. DOTween Sequence로 "손 뻗기 → 몸 끌어올리기" 연출

### 주요 설계 변경 히스토리
1. **매달리기 트리거 방식 재검토**: (1) 지상에서 Space 1회 자동 등반 → (2) 공중에서 모서리 근접 시 자동 매달림 → (3) 지상 Space(점프) 후 공중에서 Space 재입력 시 등반, 세 가지를 비교해 **3번**으로 확정 (의도치 않은 오발동 방지 + 구현 리스크가 2번보다 낮음)
2. **손 뻗기 목표 위치 버그 수정**: 처음엔 최종 착지 위치 기준으로 손을 뻗어 필요 이상으로 멀리 뻗는 문제 → 실제 감지된 턱 모서리 지점(`hit.point`)을 별도로 반환해 그 지점을 목표로 수정
3. **대각선 접근 시 벽 통과 버그 수정**: `transform.DOMove`가 콜라이더 충돌을 무시하고 좌표를 직접 이동시켜 벽을 뚫는 문제 발생 → `DOTween.To`로 진행률(0~1)만 트윈시키고 `OnUpdate`에서 매 프레임 `CharacterController.Move(delta)`로 이동하도록 변경 (이징/타이밍 느낌은 유지하면서 물리 충돌 검사를 거치게 함)
4. 손 뻗기 애니메이션에도 `Ease.OutQuad` 명시적 적용

### 관전 카메라 / UI
- `OverviewCamera`: 아레나를 위(또는 z=-40 등 먼 거리)에서 조망하는 고정 카메라
- 카메라가 비추는 화면 크기를 FOV/거리로 역산해서, 아레나가 화면에서 차지하는 비율(세로 거의 꽉 참, 가로 약 25.6%)을 계산
- `OverviewUI`(Canvas 하위, Screen Space): 좌우 스카이박스 노출 영역(각 714px)을 임시 색상 패널로 채움, 추후 이미지로 교체 예정

### 테트로미노 프리팹
- 표준 테트리스 7종(I, O, T, S, Z, J, L) 형태로 단위 큐브(크기 3) 조합 프리팹 생성
- 표준 테트리스 색상 적용 (하늘/노랑/보라/초록/빨강/파랑/주황), URP/Lit 셰이더 사용
- 저장 위치: `Assets/_Game/JooHwan/02. Prefabs/TetrisBlocks/`
- 머테리얼 저장 위치: `Assets/_Game/JooHwan/04. Art/Materials/`

## 3. 작업 방식 관련 피드백 (기억해둘 것)

- **플레이테스트는 본인이 직접 진행** — Claude Code가 Play 모드로 임의 테스트하지 않음
- **스크린샷 촬영도 최소화** — 에디터에서 직접 확인 가능한 사항은 스크린샷 대신 상태를 설명하고 사용자가 직접 확인하도록 요청
- 3D 모델(아트 에셋) 적용 전, 기본 프리미티브로 맵/구조를 먼저 완성하는 순서로 진행 중
- 이름 충돌 이슈: Unity Visual Scripting 예제 패키지에 `PlayerController`라는 동일 이름 클래스가 존재해, MCP 툴의 이름 기반 컴포넌트 검색 시 잘못된 타입이 붙는 문제가 있었음. 이런 흔한 이름은 향후에도 주의 필요 (필요 시 `execute_code`로 어셈블리 내 정확한 타입을 찾아 직접 조작)

## 4. 세션 1 종료 시점 남은 작업 (참고용, 세션 2에서 대부분 진행됨)

- 테트리스: 7레인 낙하 블록 고정 시퀀스 시스템, 낙하/착지 로직, 끼임 판정(사망), 관전 모드 다음 블록 프리뷰 UI, 클리어 조건(EXIT), HUD(높이 표시)
- 팩맨: 씬부터 미착수 — 1인칭 조준 투척, 파워펠릿 소지/리젠, 고스트 NavMesh 순찰+추격, 발광 사이클 셰이더, 클리어 조건
- 핀볼: 씬부터 미착수 — 1인칭 이동+점프, 범퍼, 코인 수집, 목숨 시스템, 낙사 리스폰, 클리어 조건
- 공통: 허브 내 미니게임 선택 메뉴(시작/설명 버튼, 클리어 표시), 공통 HUD 규격화, ESC 일시정지 캔버스

---

## 5. 세션 2 — 스코프 변경 및 테트리스 핵심 시스템 구현

### 스코프 변경 — 핀볼 제외
- 핀볼이 "범퍼+고정 발판"으로 단순화되며 테트리스(발판 밟고 올라가는 점프 액션)와 코어 동작이 겹쳐, "여러 장르를 맛본다"는 재미가 약화된다고 판단
- 개발 기간 내 테트리스+팩맨 2개의 완성도에 집중하기로 결정, `미니게임파티_세부기획.md`에 반영

### 미니게임 선택 구조 변경 — 자유 선택 → 연속 흐름
- 처음엔 "게임 선택 UI(테트리스/팩맨 좌우 분할)"를 만들었으나, "허브에서 미니게임 하나 골라 들어오면 그 안에서 또 선택"하는 구조가 다른 팀원들의 씬 구조와 일관성이 떨어진다는 문제 제기
- **인트로 화면(중앙 패널, 시작/설명 버튼) → 테트리스 → (문 상호작용, 추후) → 팩맨**으로 이어지는 연속 흐름으로 재설계
- 관련 스크립트: `MiniGameFlowManager`(씬 전환/페이드/카운트다운 총괄), `GameSelectUI`(인트로 UI), `FadeCanvas`(페이드 인/아웃), `CountdownUI`(3,2,1 카운트다운)

### 테트리스 낙하 시스템
- `TetrisFallSequence`(ScriptableObject, lane+prefab 리스트) + `TetrisFallSequencer`(순서대로 스폰, 착지 후 딜레이 뒤 다음 블록)
- `FallingBlock`: Kinematic Rigidbody로 등속 낙하, 셀 단위 개별 `OverlapBox`로 착지 판정(뭉뚱그린 전체 바운딩 박스 판정은 요철 블록에서 오작동 → 셀별 판정으로 수정), 착지 시 정확한 표면 높이로 위치 스냅
- 레이어 3종 분리: `TetrisFalling`(낙하 중, Player와 물리충돌 꺼짐) / `TetrisLanding`(착지 후 발판, Player와 충돌 켜짐) / `TetrisPinTrigger`(끼임 감지 전용, 다른 모든 레이어와 충돌 없음)
- 끼임 판정: 블록 전체를 감싸는 별도 Trigger(`pinTrigger`)로 Player 겹침을 감지, 일정 시간(`pinDeathDelay`) 지속되면 사망 — 착지 완료 후에는 `pinTrigger.enabled = false`로 꺼서 안전한 발판에서는 오탐 안 나게 처리

### 테트로미노 프리팹 버그 수정
- **회전 프리팹의 근본 문제**: J/L/S/Z(비대칭 모양)와 T(2)/(4)를 "셀 좌표는 그대로 두고 루트만 Z축 90/180/270도 회전"시켜 만들었더니, 실제로는 피벗(로컬 원점) 기준 셀이 음수 좌표 영역까지 걸치면서 레인 스폰 위치·착지 판정이 어긋나 "절반만 들어가다 멈추는" 현상 발생
- 해결: 문제가 된 회전본들의 셀 좌표를 직접 재계산해 프리팹 재생성 (루트 회전 0도, 전 셀 좌표 0 이상)
- J/L 이름이 처음부터 뒤바뀌어 있던 것도 이름 교체로 수정 (도형 자체는 문제없이 좌우 대칭 구조였음)
- 착지 판정 시 표면과 셀 사이 0.1~0.2 겹침 발생 → 착지 확정 순간 정확한 표면 높이로 `MovePosition` 보정 추가

### 카메라 흔들림 (착지 임팩트)
- `CameraShake`: Perlin Noise 기반 위치/회전 흔들림, 정적 리스트로 여러 카메라(1인칭+관전) 동시 지원, `Awake` 시 원래 로컬 위치/회전을 저장해두고 그 기준으로 오프셋을 더하는 방식(고정 카메라에도 재사용 가능)
- 버그: 카메라가 `SetActive(false)`→`(true)`로 재활성화될 때 흔들림 상태가 얼어붙은 채 남는 문제 → `OnEnable`에서 상태 리셋
- `FallingBlock.Land()`에서 `CameraShake.ShakeAll()` 호출

### 사망/재시작/클리어 플로우
- `TetrisGameManager` 신설: 끼임 사망 시 전체 일시정지(`FallingBlock.GlobalPaused`) + 조작 잠금 + 사망 팝업(재시작/게임 설명/허브로 나가기 3버튼)
- 재시작: `TetrisFallSequencer.ResetSequence()`(스폰된 블록 전부 파괴) + Player 시작 위치 리스폰 + 카운트다운 후 재개
- 카운트다운 중에도 조작 가능하도록 설계(숫자는 연출일 뿐 조작 제한 아님) — 처음엔 최초 시작과 재시작의 잠금 해제 타이밍이 달라 불일치했던 버그를 카운트다운 시작 "전"에 잠금 해제하는 것으로 통일
- EXIT 트리거(클리어) + 문 상호작용 + 팩맨 진입은 **의도적으로 보류** — 나중에 "위쪽을 막고 팩맨으로 넘어가는 통로"를 만들 때 한 번에 진행하기로 함. 클리어 시 연출은 별도 성공 UI 없이 "CLEAR" 텍스트만 짧게 보여주고 조작 해제하는 것으로 방향만 확정

### 트러블슈팅 — 커서/UI 관련
- `PlayerController.OnEnable()`이 무조건 커서를 잠그고 숨기게 되어 있어, `CameraRig`가 관전 전환 시 `PlayerController.enabled`를 껐다 켤 때마다 사망 팝업 등으로 풀어둔 커서가 다시 숨겨지는 문제 → `OnEnable`이 현재 `controlsLocked` 상태를 재적용하도록 수정
- Canvas 자식 순서(렌더링 순서) 때문에 사망 팝업 위에서 연 설명 팝업이 뒤에 가려지는 문제 → `DescriptionPopup`을 `SetAsLastSibling()`으로 최상단 이동
- `ExitTrigger.cs`를 만들다 작업을 중단했는데, 존재하지 않는 메서드를 참조한 상태로 파일만 남아 컴파일 에러 발생 → 파일 삭제 후에도 Unity가 이전 컴파일 에러를 계속 재표시하는 현상 발생, `CompilationPipeline.RequestScriptCompilation(CleanBuildCache)`로 캐시를 강제 정리해서 해결 (일반 `AssetDatabase.Refresh`만으로는 해소 안 됨)

### 아레나 규격 변경 + 비주얼
- 벽/천장 높이 45 → 50으로 확장 (블록 스폰 높이 45와의 여유 확보), 천장 신설 (Default 레이어, 순수 물리 차단용)
- 오락실 게임기 내부 분위기: Ambient Light를 Flat 모드의 어두운 색으로, 벽/바닥/천장 전용 어두운 머테리얼(`Mat_TetrisArena_Dark`)로 교체, 벽-바닥 경계에 네온 라인(Emission 머테리얼, 콜라이더 없음) 배치, 아레나 중앙에 은은한 Point Light 추가
- 관전 모드 진입/해제 시 `CameraRig`가 Ambient Light를 밝게/어둡게 전환 (어두운 상태에서는 블록 색 구분이 안 되는 문제 해결) — 관전 사이드 UI에는 추후 실제 테트리스 이미지를 넣을 예정

## 6. 세션 2 종료 시점 남은 작업

- 테트리스: EXIT 트리거(클리어), CLEAR 연출, 문 상호작용 + 팩맨 진입 연결 — 팩맨 제작과 함께 진행 예정
- 팩맨: 씬부터 미착수
- 공통: 허브 씬 자체가 아직 없음(다른 팀원 담당, Jihoon 폴더에 `Main_Logo` 씬만 존재) — "허브로 나가기" 등은 그때 연결
- ESC 일시정지 캔버스: 미착수

---

## 7. 세션 3 — 파스텔 아트 전환, 테트리스-팩맨 연결, 팩맨 핵심 시스템 구현

### 아트 톤 전면 교체 — 오락실 네온 → 파스텔
- 허브(다른 팀원 제작)가 파스텔톤의 아늑한 다락방 분위기로 확정되면서, 기존 테트리스의 "어두운 오락실 내부 + 시안 네온" 컨셉이 팀 전체 톤과 이질적이라고 판단해 파스텔 톤으로 전면 교체
- `Mat_TetrisArena_Dark`(어두운 벽/바닥) → 밝은 베이지 파스텔로 색상 변경, `Mat_NeonLine_Cyan` → 따뜻한 옐로우 글로우로 변경 (파일명은 이후 사용자가 에디터에서 각각 `Mat_TetrisArena_Pastel`/`Mat_GlowLine_Warm`으로 직접 변경)
- 테트로미노 7종 블록 색상도 원색에서 파스텔톤으로 재조정
- `CameraRig`의 Ambient Light 값(1인칭/관전 모드 각각)을 어두운 톤 → 밝고 따뜻한 톤으로 변경
- 아레나 중앙 Point Light(`ArenaAmbientLight`)는 관전 카메라에서 동그랗게 튀어 보여 제거 — Ambient Light만으로 충분히 밝음
- 세부기획서(`미니게임파티_세부기획.md`)의 "오락실 게임기 내부" 문구를 "파스텔 톤 동화풍"으로 수정

### 팩맨 맵 확장 및 대칭 편집
- 초기 11x11 미로를 **15x15로 확장** (기존 "사방 대칭+중앙 십자 통로+모서리 작은 방" 디자인 언어 유지한 채 비율만 확장), Walls/Floor/Ceiling/GlowLines/PowerPellets/PacmanEntryPoint 전부 재계산
- 사용자가 좌상단 1개 사분면을 나선형(소용돌이) 패턴으로 직접 수정 → 나머지 3개 사분면에 좌우/상하/대각 대칭 변환으로 자동 적용
- 통로 노출면 기준으로 자동 배치되는 `GlowLines`(파스텔 웜톤 발광 라인)를 미로 형태 변경 때마다 재생성하는 방식으로 유지

### 테트리스-팩맨 물리적 연결
- 기존엔 팩맨 맵이 테트리스와 뚝 떨어져 SetActive로만 전환되던 구조 → **문(EXIT 예정 위치) 앞에 실제로 인접 배치**하는 방향으로 변경, 이후 사용자 요청으로 문~팩맨 입구 거리를 기존의 2배로 확장하고 그 사이를 테트리스 출구 크기에 맞춘 복도(`DoorCorridor`, 바닥+천장+좌우벽)로 감쌈
- 팩맨 미로 입구 자리(`Wall_0_7`, 미로 중앙 열)는 **평소 메쉬 없이 콜라이더만 남겨** 문 너머로 미로 내부가 보이되 걸어서 통과는 못 하게 처리 (`PacmanEntranceWall` + 감지 전용 자식 트리거 `EntranceInteractTrigger`)
- 상호작용(F키, `Interact` 액션 신규 추가) 시: 벽 메쉬를 다시 채워 완전히 막고 → `MiniGameFlowManager`가 화면이 어두워진 직후(카운트다운 전)에 플레이어 텔레포트 + 고스트 워프를 함께 처리 → 페이드인 → 카운트다운(3,2,1) → 그 직후에야 고스트가 실제로 움직이기 시작

### 미니게임 전환 구조 재설계 — SetActive → 상태 기반 On/Off
- 테트리스/팩맨이 하나로 이어지는 연속 흐름이라는 점을 반영해, `TetrisRoot`/`PacmanRoot`를 더 이상 `SetActive`로 껐다 켜지 않고 **항상 활성 상태로 유지**
- `MiniGameFlowManager`에 `MiniGameState`(MainUI/Tetris/Pacman) 개념 도입, 상태 전환 시 각 게임의 카메라/조작/게임로직/고스트/크로스헤어/하트UI를 개별적으로 On-Off
- 테스트 편의를 위한 `debugStartInPacman` 옵션 추가: 인트로/테트리스를 건너뛰고 "테트리스 이미 클리어, 문 앞 복도, 팩맨 아직 비활성" 상태로 바로 시작 (실제 게임 흐름 완성 후에는 꺼둬야 함 — **TODO**)

### 파워펠릿 시스템
- `PowerPellet`(맵 배치, 트리거 습득) / `PelletThrower`(최대 2개 소지, 좌클릭 투척, 양손 시각 표시) / `ThrownPellet`(비행, 충돌 시 소멸) / `PelletSpawner`(4모서리+중앙 5개 관리, 전부 습득 시 전체 리젠)
- 투척 스폰 위치를 카메라 그대로 두면 Player 자신의 CharacterController와 겹쳐 즉시 충돌 판정이 나던 버그 → 카메라 앞으로 오프셋 추가
- 화면 중앙에 원형 크로스헤어(에임 포인터) 추가, 팩맨 상태에서만 표시

### 고스트 AI (5마리)
- `Ghost`(공통 로직: NavMeshAgent 이동, 순찰↔추격 상태, 발광 주기, 피격 판정) + `IGhostMovementSource` 인터페이스로 이동 패턴 분리
- `GhostPatrolLoop`: 4마리가 각자 사분면 나선 통로를 순환 (이미지에서 추출한 18개 웨이포인트 경로를 4구역에 대칭 적용)
- `GhostPingPong`: 1마리가 테트리스-팩맨 연결 통로에서 왕복. 팩맨 진입 전에는 통로 끝(테트리스 쪽을 바라보는 대기 자세)에서 정지해 있다가, 진입 확정 시 원래 왕복 시작 지점으로 워프 후 순찰 시작
- NavMesh 베이크: `NavMeshSurface`를 `PacmanRoot/Floor`에 배치, `collectObjects=Volume`으로 팩맨 바닥 영역만 정확히 감싸는 박스 범위로 한정(다른 미니게임 영역이 섞여 들어가는 문제 방지), 모든 벽에 `NavMeshModifier(Not Walkable)`를 명시적으로 부착해 베이크 알고리즘의 경계 근사 오차(벽 내부에 얇은 통로가 남는 문제)를 방지
- 플레이어 탐지: 거리+시야각 기반, 추격 중 놓치면 순찰 복귀
- 발광(공격 신호) 주기: 평소(위협 상태, 접촉 시 플레이어 피격) ↔ 발광(파워펠릿 처치 유효, 접촉 시에도 피격은 그대로 유지 — 발광 중 안전하다는 통념을 사용자 요청으로 제거) — 개별 고스트마다 인스펙터에서 지속시간 조절 가능
- 피격 판정 트러블슈팅: 고스트 콜라이더를 트리거로 바꾸면서(플레이어 피격 감지용) 기존 `ThrownPellet.OnCollisionEnter`(비트리거 전용)가 파워펠릿 명중을 못 잡게 된 문제 → `OnTriggerEnter`도 함께 처리하도록 수정
- 처치 연출: 커스텀 디졸브 셰이더 대신 URP/Lit 머테리얼을 런타임에 투명 모드로 전환해 알파를 펄린 노이즈로 흔들며 0까지 줄이고 스케일도 축소(`GhostDefeatEffect`), 동시에 고스트 색과 동일한 파티클 버스트 재생 — 처치 후 재시작 시 되살아나야 하므로 `Destroy` 대신 `SetActive(false)`로 변경
- 발광 시 몸 색 구별 문제: 처음엔 모든 고스트가 동일한 노란색으로 덮여 구별이 안 됨 → 원래 색과 섞는 방식 시도했으나 파스텔 특성상 포화되어 흰색으로 보임 → 색조를 노란쪽으로 살짝 이동하는 HSV 방식도 시도했으나 Red/Pink/Orange의 원래 색조가 서로 가까워 여전히 구별 안 됨 → 최종적으로 **색조는 완전히 그대로 유지하고 채도/명도만 강화**하는 방식으로 확정("자기 색 그대로 더 밝고 진하게"). 오라 파티클(고스트 고유 색, 발광 중에만 재생)도 추가로 병행
- 실제 아트 모델 적용 시 이 발광/색상 로직은 재조정이 필요할 수 있음 — **TODO**

### 목숨 시스템 (팩맨 전용)
- 테트리스는 목숨 개념 없음(즉시 재시작), 팩맨은 **목숨 3개** 시스템으로 차별화 — 장르 특성과 재도전 비용 차이를 반영한 의도적 결정
- `PlayerHealth`: 최대 3, 피격 시 감소 + 무적시간(1.5초, 연속 피격 방지), 0이 되면 테트리스 사망 팝업과 **동일한 UI**를 재사용해 표시하되 재시작은 팩맨 쪽으로 분기
- `HeartUI`: 왼쪽 위 하트 3개(빨강=생존/검정=소진), 지금은 실제 스프라이트가 없어 코드로 생성한 단색 원형 PNG(`Heart_Full.png`/`Heart_Empty.png`)로 임시 대체 — 추후 실제 하트 에셋으로 교체 예정
- 재시작(`MiniGameFlowManager.RestartPacman`) 시 목숨 리셋뿐 아니라 파워펠릿 전체 리젠, 소지 파워펠릿 초기화, **처치됐던 고스트 전원 부활 + 원래 순찰 시작 위치로 재워프**까지 한 번에 처리 (고스트가 죽은 자리에서 그대로 부활하던 버그, `GhostPatrolLoop.BeginPatrol()`이 목적지만 정할 뿐 실제 워프를 안 해서 생긴 문제를 수정)

### 트러블슈팅 — 그 외
- 메인 UI 화면에서 마우스 클릭이 안 먹는 문제: 어떤 게임의 `PlayerController`도 활성화되지 않은 상태에서는 커서 상태를 아무도 갱신하지 않아, 이전 세션의 잠긴 커서 상태가 남아있었음 → `MiniGameFlowManager`가 메인 UI 진입 시 커서를 명시적으로 보이게 처리
- Unity 에디터가 포커스를 잃으면 Play 모드에서도 프레임이 거의 진행되지 않아, `execute_code`로 상태 변화를 확인할 때 혼란이 있었음 — 스크린샷 캡처가 프레임을 강제로 진행시키는 부작용이 있다는 것을 파악

## 8. 세션 3 종료 시점 남은 작업 (세션 4에서 1,2,3 완료)

- ~~팩맨 진입 시 점프 비활성화~~ — 세션 4에서 완료
- ~~클리어 조건(고스트 5마리 전멸) 연결~~ — 세션 4에서 완료
- ~~ESC 일시정지 캔버스~~ — 세션 4에서 완료
- 허브 씬 연결: 아직 허브 씬 자체가 없어 대기 중 (범위 밖)
- `debugStartInPacman` 디버그 옵션: 팩맨 로직이 최종 완성되면 반드시 꺼야 함
- 하트 UI 스프라이트: 임시 단색 원형 → 실제 아트 에셋으로 교체 필요 (범위 밖)
- 고스트 발광 시 색상 표현: 실제 고스트 모델(아트 에셋) 적용 시 재조정 필요할 수 있음 (범위 밖)
- CLAUDE.md 위반사항(ProjectSettings 변경, 레이어 슬롯 조율) — 팀장 공유는 사용자가 직접 처리 예정, 아직 미해결

---

## 9. 세션 4 — 1차 프로토타입 완성(점프/클리어/일시정지) + 대규모 리팩토링 + 코드 리뷰

### 1차 프로토타입 완성 (남은 작업 1,2,3)

**팩맨 진입 시 점프/등반 비활성화**
- `PlayerController.SetJumpEnabled(bool)` 추가 — 기존 `jumpEnabled`가 private라 외부에서 런타임 제어가 불가능했던 문제 해결
- `ClimbController`는 별도 처리 없이 자동으로 막힘 — `OnJumpPerformed`가 `PlayerController`와 동일한 `InputActionAsset`의 Jump 액션을 구독하므로, 액션 자체를 Disable하면 두 컴포넌트 모두 동시에 막힘 (액션 인스턴스가 공유되는 특성)
- `TetrisGameManager.SetJumpAllowed()` → `MiniGameFlowManager.ApplyState()`에서 `target != Pacman`일 때만 허용하도록 연결

**클리어 조건(고스트 5마리 전멸) 연결**
- `Ghost`에 `Defeated`(`System.Action<Ghost>`) 이벤트 추가, 처치 시점에 발행
- `MiniGameFlowManager`가 5마리 전부 구독해 전멸 여부 판정 → 팩맨 맵 중앙에 보상 상자(큐브, 황금색) 활성화
- `RewardBoxTrigger`: F키(Interact) 상호작용으로 상자를 엶. `IPlayerItemSave` 인터페이스로 아이템 획득 여부 판단을 위임 — 다른 팀원이 만들 `PlayerData` 스크립트가 이 인터페이스만 구현하면 자동 연동되도록 설계. 아직 구현체가 없는 동안은 `PlayerItemSaveLocator`가 더미로 대체해 에러 없이 테스트 가능
- `RewardPopupUI`: 처음엔 텍스트만 잠깐 띄우는 방식이었으나, 아래에 메인씬 복귀 버튼이 필요해져 `DeathPopup`과 동일한 패널형(제목+버튼)으로 변경
- 트러블슈팅: 고스트가 파워펠릿에 맞아 디졸브 연출(파티클+축소) 중에도 콜라이더가 살아있어 플레이어가 지나가면 피격되는 버그 → 처치 즉시 콜라이더만 비활성화(시각 연출은 유지)하는 방식으로 수정

**ESC 일시정지**
- `PauseUI`: 기존 Input Actions에 이미 정의돼 있던 Pause(ESC) 액션을 처음으로 실사용. 테트리스 낙하/팩맨 고스트 이동을 실제로 멈추는 방식(`Time.timeScale`이 아니라 기존 `SetTetrisGameplayPaused`/고스트 `SetActive` 패턴 재사용 — 코루틴 기반 페이드/카운트다운까지 함께 멈추는 부작용을 피하기 위함)
- 버튼 3개(계속하기/게임 설명/메인씬으로) 중 계속하기만 실동작, 게임설명은 기존 설명 팝업 재사용, 메인씬은 허브 미구현으로 자리표시자
- 트러블슈팅: 카운트다운(3,2,1) 진행 중 ESC를 누르면 카운트다운은 코루틴이라 계속 흘러가는데 게임 로직만 멈춰버리는 불일치 → 카운트다운 중에는 ESC 자체를 무시하도록 처리(짧은 연출 구간이라 완벽한 일시정지보다 비용 대비 효율적 판단)
- 트러블슈팅: 일시정지/보상 팝업이 뜬 상태에서 버튼을 클릭하는 좌클릭이 그대로 Throw 액션으로도 들어와 파워펠릿이 오발사되는 문제 → `PlayerController.SetControlsLocked(true)` 시 Interact/Throw 등 관련 액션도 함께 잠기도록 수정

### 리팩토링 1단계 — 씬 하이어라키/에셋 폴더 정리
- 팩맨 맵 Walls(116→26개), GlowLines(188→68개)를 이어진 구간 단위로 병합 — 최종 비주얼이 색/텍스처 위주로 예정되어 칸 단위 분리의 이점이 크지 않다고 판단해 진행. 이름 규칙(`Wall_H_r{row}_c{시작}-{끝}` 등)과 하이어라키 순서(H 전체→V 전체)도 함께 정리
- `PingPongPoints` 하위 오브젝트 로컬 좌표 정리 — 부모(`PacmanRoot`)가 오프셋을 가진 상태에서 자식 좌표가 그 오프셋을 역산해 상쇄하고 있어 음수로 보이던 것을, 월드 좌표를 유지한 채 부모를 (0,0,0)으로 맞추고 자식 좌표를 재계산
- `SelectSceneCamera` 제거 — cullingMask=0인 미완성 카메라였는데, 실제로는 MainUI 상태에서 활성 카메라가 하나도 없어지는 문제(화면 렌더링 공백)를 막는 임시 땜빵이었음. 근본 원인(`TetrisGameManager.SetGameActive()`가 1인칭 카메라까지 꺼버림)을 고쳐서 제거
- Canvas 하위 UI 13개를 MainMenuUI/TetrisUI/PacmanUI/CommonUI 4개 그룹으로, PacmanRoot 하위를 PacmanArena/Pellets/GhostGroup으로 정리
- 에셋 폴더: `01. Scripts`를 Core/Player/Tetris/Pacman/UI로, `02. Prefabs`의 Pellet 관련을 Pellets 폴더로, `04. Art/Materials`를 TetrisBlocks/Tetris/Pacman/Ghosts/Pellets로, Sprites의 Heart를 Heart 폴더로 분류

### 리팩토링 2단계 — 명명법 통일
- Unity 공식 C# 스타일 가이드 기준으로 전체 스크립트 32건 위반 수정 — bool 매개변수/필드에 is/has 접두사 누락(23건, `active`→`isActive` 등), `On` 접두사 오용(이벤트를 발생시키는 메서드가 아닌데 붙어있던 `OnExitReached`/`OnPlayerPinned`/`OnHitByPellet`/`OnPelletPickedUp`→`Handle*`로, 반대로 진짜 이벤트였던 `Ghost.OnDefeated`→`Defeated`로), `FallingBlock.GlobalPaused`→`IsGlobalPaused`, `PlayerController.ControlsLocked`→`AreControlsLocked`
- `TetrisFallSequence`(ScriptableObject)의 `lane`/`prefab`/`entries` 필드명 변경은 기존 `.asset` 데이터(테트리스 낙하 시퀀스) 유실 위험이 있어, 변경 전 `TetrisFallSequence_01.asset`의 전체 항목을 별도 백업해두고 `[FormerlySerializedAs]`로 하위 호환을 유지하며 안전하게 진행. 실제로 데이터 유실 없이 21개 항목 정상 로드 확인

### 코드 구조 리팩토링 — 스크립트 순회 리뷰
파일별로 역할/필드/메서드를 설명받으며 하나씩 검토, 발견된 문제를 그때그때 수정하는 방식으로 진행.

- **`GhostHittable` 추상 클래스 제거**: 구현체가 `Ghost` 하나뿐이고 클래스명 자체가 이미 Ghost 전용임을 암시해, "확장성을 위한 추상화"라는 근거가 이름과 모순됨을 확인 → `ThrownPellet`이 `Ghost`를 직접 참조하도록 단순화
- **`CameraShake`의 안 쓰이는 오버로드 제거**: `ShakeAll(float, float, float)`/`Shake(float, float, float)`가 실제 호출처 없이 존재하던 죽은 코드 → 제거
- **`ClimbController`가 `PlayerController`의 Move/Look 액션을 직접 Disable/Enable하던 것을 캡슐화**: `PlayerController.SetMovementEnabled(bool)` 추가(내부적으로 `enabled`만 바꾸면 `OnEnable`/`OnDisable`이 액션까지 처리해줌을 확인해 단순화), `ClimbController`는 이 API만 호출하도록 변경. Jump 액션은 제어 대상이 아니라 구독 대상이라는 성격 차이를 근거로 그대로 유지
- **`CameraRig`의 콜백 계층 단순화**: `OnCameraSwitchPerformed → ToggleCamera` 2단계 분리가 "다른 트리거 방식(버튼 등) 도입 가능성"을 근거로 존재했으나, 이 게임에서 카메라 전환은 입력 액션 외의 방식으로 절대 트리거되지 않는다고 확정 → 하나로 병합
- **`TetrisGameManager` 3분할**: 원래 테트리스 전용이었던 클래스가 팩맨 쪽 요구사항(크로스헤어/점프허용/사망팝업)까지 계속 흡수해온 것을 발견 — 이름과 실제 책임이 불일치. 앞으로 팩맨 공격/피격 피드백 기능이 더 늘어날 예정이라는 점을 고려해 `SharedGameplayManager`(테트리스/팩맨 공용 자원: 플레이어/카메라/사망팝업), `TetrisGameManager`(테트리스 전용으로 축소), `PacmanGameManager`(팩맨 전용 신규)로 분리. UI 버튼들의 `onClick` 연결(사망/일시정지/보상 팝업)이 스크립트 교체로 깨져 전부 재연결 필요
- **분리 과정에서 발견한 추가 버그**: `CameraRig.ToggleCamera()`가 조작 잠금 여부를 확인하지 않아, 테트리스에서 일시정지/사망 팝업 위 좌클릭이 카메라 전환으로도 처리되던 문제 → `PelletThrower`가 이미 쓰던 `AreControlsLocked` 체크 패턴을 동일하게 적용해 수정
- **크로스헤어 표시 로직 재배치**: `CameraRig`가 "관전 여부를 안다"는 이유로 크로스헤어까지 관리하고 있었는데, 실제로는 테트리스에서 크로스헤어 자체가 불필요하고 팩맨에서 관전 모드가 존재하지 않아(Tab이 원래 안 막혀있던 버그까지 함께 발견) 그 조건이 죽은 로직이었음을 확인 → `CrosshairController` 신규 컴포넌트로 분리, `CameraRig`에는 `SetCameraSwitchAllowed`(팩맨에서 카메라 전환 자체를 막는 것)와 `ResetToFirstPerson`(재진입 시 관전 모드가 남아있는 어긋난 상태를 강제 리셋)만 남김

### 트러블슈팅 — 씬 리네임 및 스크립트 GUID
- `PlayerHealth` → `PacmanPlayerHealth` 클래스명 변경(다른 팀원과 이름 충돌) 과정에서, 파일명을 에디터 밖에서 바꾸며 `.meta`가 새로 생성되어 GUID가 바뀌는 문제 발생 → 씬의 해당 컴포넌트가 Missing Script로 깨짐 → git 히스토리에서 원래 GUID를 찾아 `.meta` 파일을 직접 복구해 해결. 이후 스크립트 파일명 변경은 반드시 Unity 에디터의 Project 창에서 F2로 진행하기로 함
- 씬 파일명을 `JooHwan_MiniGame_Tetris` → `JooHwan_MiniGame_Main`으로 변경 (에디터에서 직접 진행, 코드에 문자열로 씬 이름을 참조하는 곳이 없어 안전하게 완료)

### 입력 매핑 변경
- 테트리스의 카메라 전환(Tab)을 좌클릭으로 변경, 팩맨의 투척(좌클릭)과 물리 키를 공유 — 두 액션이 상태별로 상호 배타적으로 Enable/Disable되어 있어 충돌 없이 자연스럽게 스위칭됨을 확인

### 논의 후 보류/메모
- `GhostPingPong.EnterIdleState`가 `IGhostMovementSource` 인터페이스에 없는데 `Ghost`가 구체 타입으로 다운캐스팅해 호출하는 부분 — 왕복형 구현체가 하나뿐이라 당장은 문제 없지만, 나중에 구현체가 늘어나면 인터페이스에 정식 편입하거나 별도 역할 인터페이스로 분리 검토
- 끼임(Pin) 판정 관련 잠재 문제 논의: (1) `SetupPinTrigger()`가 셀들의 bounding box 전체로 트리거를 만들어 L자 등 빈 칸이 있는 블록에서 실제로 블록이 없는 자리도 끼임으로 판정될 수 있음 — 게임 디자인상 낙하 중 회피가 강제되어 실전에서는 발생 안 한다고 보고 보류. (2) 죽는 방식 자체를 "낙하 중 접촉 시 사망"에서 "착지 완료 후 그 자리에 갇힌 경우만 사망"으로 바꾸는 방향 논의 — 표면에 스치기만 해도 죽는 문제가 남아있어, `Physics.ComputePenetration` 기반 침투 깊이 판정(표면 접촉과 실제 끼임을 구분)이 필요하다는 결론
- 사망 피드백 강화 아이디어: 침투 깊이가 커질수록 화면 가장자리가 어두워지는 비네트 효과 연출, URP Post Processing Volume의 Vignette를 코드로 실시간 조절하는 방식으로 구현 예정 (다음 세션에서 진행)

## 10. 세션 4 종료 시점 남은 작업

- 비네트(화면 가장자리 어두워짐) 사망 피드백 구현 — URP Vignette 사용, 침투 깊이 기반 끼임 판정(방법 C)과 함께 연동 예정
- 끼임 판정을 "착지 후 갇힘"으로 정확히 재구현 (표면 스침과 구분)
- 허브 씬 연결, 하트 UI 스프라이트, 고스트 발광 색상 재조정 — 아트 에셋/다른 팀원 작업 의존이라 범위 밖 유지
- `debugStartInPacman` 디버그 옵션 — 최종 완성 전 반드시 해제
- `GhostPingPong.EnterIdleState`의 인터페이스 미편입 문제 — 왕복형 구현체가 늘어나면 정리 검토
- CLAUDE.md 위반사항 팀장 공유 — 아직 미해결

## 11. 세션 5 — 끼임 판정 재구현, 사망/클리어 피드백, 고스트 스턴, 아트 텍스처 적용

### 끼임(Pin) 판정 재구현 — 트러블슈팅 다수
지난 세션 보류 항목이었던 "착지 후 갇힘만 사망"으로 방식을 바꾸는 작업. 예상보다 훨씬 많은 시행착오를 거쳐 최종 방식을 확정.
- **1차 시도(pinTrigger + detectCollisions 끄기)**: 착지 판정을 겹침 중엔 보류하는 방식 → 침투가 사망 임계값(80%)에 못 미친 채 계속 유지되면 착지 검사 자체가 영원히 막혀 블록이 바닥을 뚫고 무한히 낙하하는 심각한 결함 발견, 폐기
- **2차: 셀 콜라이더 자체를 낙하 중 트리거로 토글**: `OnTriggerEnter`가 "Kinematic Rigidbody의 트리거 콜라이더가 정지한 CharacterController 쪽으로 다가오는 경우" Unity에서 호출되지 않는 제약을 실측으로 확인(`Physics.ComputePenetration`으로는 실제 겹침이 잡히는데 트리거 콜백만 안 옴) → 이벤트 방식 자체를 폐기
- **최종 방식**: 트리거 이벤트에 의존하지 않고, 매 `FixedUpdate`마다 모든 `FallingBlock`이 능동적으로 `Physics.ComputePenetration`을 호출해 플레이어와의 침투 깊이를 직접 계산. `Bounds.Intersects`로 먼저 대략적인 근접 여부를 걸러낸 뒤에만 정밀 계산(겹치지 않는 콜라이더 쌍에 호출하면 NaN이나 극단값이 나오는 문제 발견 후 방어)
- **착지 순간 튕김 문제**: 셀이 솔리드로 전환되는 순간 플레이어가 깊이 파묻혀 있으면 물리 엔진이 강제로 밀어내 튕겨나가는 현상 → 착지 검사(`CheckLanding`)는 겹침 여부와 무관하게 항상 실행하고, 착지가 확정되는 순간(`Land()`) 플레이어가 겹쳐 있으면 침투율이 몇이든 상관없이 즉시 사망 처리하는 방식으로 근본 해결(그 시점 이후 게임이 멈춰 물리 반응이 발생할 기회 자체가 없어짐)
- 사망 임계값을 100%가 아니라 80%로 설정(물리적으로 100% 침투는 발생하기 어려움), 사용자 요청으로 이후 재조정 논의 없이 확정

### 사전 경고 비네트 (`TetrisDangerVignette` 신규)
- URP Post Processing Volume + Vignette로 구현. 블록이 착지까지 `dangerWarningDistance`(10m) 이내로 남았고 플레이어가 그 바로 아래 있으면 경고 시작, 접촉 후 침투율에 비례해 강도가 자연스럽게 이어짐(0.3~0.5: 접촉 전 접근, 0.5~0.7: 접촉 후 침투). 사망 플래시 등 다른 피드백이 가려지지 않도록 실제 화면 강도에는 항상 상한(0.3~0.7 구간)을 둠
- **트러블슈팅**: (1) 카메라의 `UniversalAdditionalCameraData.renderPostProcessing`이 꺼져 있어 Vignette 자체가 안 보이던 문제, (2) `Volume.profile`(런타임 게터)이 빈 인스턴스를 반환하는 문제 → `sharedProfile`을 `Instantiate()`로 복제해 별도 런타임 인스턴스로 사용하도록 수정, (3) 원본 에셋을 직접 수정해 Play 모드 종료 시 그 값이 에셋 파일에 영구 반영되던 문제 → 복제 인스턴스 사용으로 해결 + 에디터에서 Play 종료 시 강제 초기화(`EditorApplication.playModeStateChanged`)까지 추가, (4) 착지 판정 거리 계산이 "블록이 바닥에 닿기까지"가 아니라 "블록이 플레이어 몸에 닿기까지"를 재야 하는데 반대로 계산되어 있던 버그 수정
- 착지한 블록 옆에 서 있기만 해도 비네트가 발동하는 문제 → 사전 경고/접촉 판정 모두 `!isLanded`(낙하 중) 조건을 추가해 해결

### 사망/클리어 피드백 강화
- **사망**: 즉시 재시작 팝업이 뜨던 것을 0.6초 지연 + 강한 카메라 셰이크(`CameraShake`에 강도 지정 오버로드 추가) + `DeathFlashOverlay`(신규, 화면 전체 색 플래시) 재생으로 개선. 일시정지/재시작 시 비네트가 잔상처럼 남는 문제도 함께 해결
- **클리어**: CLEAR 텍스트만 뜨던 것을 밝은 색 플래시 + 가벼운 셰이크 + DOTween 기반 스케일 팝업(`OutBack`)+페이드아웃으로 강화. `DeathFlashOverlay`를 색 지정 가능하게 범용화해 재사용
- 팩맨 일반 피격(목숨 남음)에도 사망과 동일한 강도의 카메라 셰이크만 재생(플래시는 완전 사망 시에만, 구분을 위해)

### 고스트 추격/스턴
- `NavMeshAgent.speed`만 올려서는 가속에 시간이 걸려 "확 쫓아오는" 느낌이 안 남 → `acceleration`을 추격 시작 시 크게(200) 올려 즉시 최고 속도에 도달하도록 수정
- 피격 후 고스트가 그 자리에 완전 정지(스턴, 2초)하도록 추가 — 가속 붙은 추격에서 플레이어가 벗어날 틈을 줌. 스턴 중엔 탐지/추격 로직 자체를 멈추고, 스턴이 끝나면 무조건 순찰로 복귀 후 재탐지 가능

### 버그 수정 — ESC/사망 팝업 충돌
- 사망 팝업이 떠 있는 동안에도 ESC로 일시정지 패널이 겹쳐 열리는 버그 발견 → 겹쳐 열린 상태에서 계속하기를 누르면 사망으로 잠긴 조작/일시정지 상태가 강제로 풀려버리고, 재시작을 눌러도 일시정지 패널이 안 사라지는 연쇄 문제로 이어짐 → `PauseUI.Pause()`에 `AreControlsLocked` 체크 추가해 근본 해결

### 디버그: 클리어 연출 즉시 확인
- 기존 `isDebugStartInPacman`은 `DebugForceClearedState()`로 클리어 연출 자체를 건너뛰어 확인이 불가능했음 → 별도 디버그 옵션(`isDebugStartAtTetrisClear`) 추가, EXIT 트리거 바로 아래(뒤쪽으로 약간 떨어진 지점)에 임시 디딤대를 코드로 생성해 실제로 걸어가 EXIT을 밟는 정식 흐름으로 클리어 연출을 확인할 수 있게 함 (디딤대는 씬에 영구 저장되지 않고 런타임에만 생성)

### 아트 — 테트리스/팩맨 벽 텍스처
- GPT Image로 시임리스 타일 텍스처 제작 시도 — 여러 차례 반복하며 얻은 교훈: (1) "seamless tileable"이라는 문구만으로는 완벽한 타일링이 보장되지 않아 매번 2x2로 이어붙여 픽셀 단위로 좌우/상하 경계 차이를 측정해 검증하는 과정이 필요했음, (2) "growth rings"(나이테) 같은 키워드가 나무 단면(동심원) 구도를 유도해 세로로 긴 벽에 반복하면 착시가 발생 → "vertical wood grain", "no circular rings" 등으로 명시해야 세로 결 텍스처를 얻을 수 있었음, (3) "떨어지는 나뭇잎" 같은 서술적 표현이 원근감 있는 장면(소실점, 결이 좁아지는 구도)을 유도해 타일링이 깨지는 경우가 많아, 기존 텍스처 이미지를 레퍼런스로 첨부한 이미지 편집(Edit Image) 방식이 텍스트 프롬프트보다 훨씬 안정적이었음
- 세계관("씨앗 키우기", 플레이어 입장은 나무)에 맞춰 아트 컨셉을 "오락실 기기 내부"에서 "나무 속을 타고 올라가는 테트리스 / 나뭇잎+열매 미로를 오가며 무당벌레를 잡는 팩맨"으로 전환
- 테트리스 벽을 넓은 벽(Back/Front, 나무 안쪽 결)과 좁은 기둥(Front_Below/Above, Left/Right)으로 재질을 분리해 각각 다른 텍스처와 Tiling을 적용 — 하나의 재질을 모든 벽이 공유하면 폭이 다른 벽마다 결 밀도가 안 맞아 보이는 문제를 해결
- 팩맨 GlowLines 색을 웜 옐로우에서 원색 블루로 변경(테트리스와 색 온도로 공간을 구분), 팩맨 벽은 도트(펠릿과 혼동 우려로 폐기) 대신 블록 구획선 패턴 논의 후, 최종적으로 나뭇잎+열매 모티프로 방향 확정(다음 세션에서 이어서 제작)

## 12. 세션 5 종료 시점 남은 작업

- 팩맨 벽 텍스처(나뭇잎+열매) 제작 및 적용 — 색/구도 확정까지는 했으나 최종 텍스처는 미제작
- 사운드 작업 — VARCO3D 등에서 클립 준비 후 `AudioManager`/`SoundData`에 연결 예정 (파워펠릿 처치/획득/투척)
- 테트리스 시퀀스 다양화(3개 랜덤 선택) — 재시작 시 매번 다시 랜덤 재선택하는 방향으로 논의했으나 미착수
- 보상 상자 파티클+UI 피드백 — 미착수
- 비네트 세로 방향 텍스처 이음새가 완벽하지 않은 부분 존재 — 사용자 판단으로 우선 진행, 필요시 추후 재작업
- `isDebugStartInPacman`, `isDebugStartAtTetrisClear` 디버그 옵션 — 최종 완성 전 반드시 해제
- CLAUDE.md 위반사항 팀장 공유 — 아직 미해결

---

## 13. 세션 6 — 재시작/설명 UI, 사망 팝업 정리, 팩맨 벽·바닥·천장 아트 적용

### 재시작 전 설명 UI (`PreGameDescriptionUI` 신규)
- 요구사항: 재시작(ESC "처음부터" + 사망 시 재시작) 시 카운트다운 전에 게임 설명 UI를 먼저 보여주고, 확인 버튼을 눌러야 카운트다운이 시작되도록 함. 테트리스/팩맨 각각 별도 UI(`TetrisDescriptionPopup`/`PacmanDescriptionPopup`)로 분리
- `MiniGameFlowManager.PlayCountdownWithDescription()`, `TetrisGameManager.RestartFromDeath()` 양쪽에 동일 패턴 적용 — 대상 게임에 맞는 설명 UI를 먼저 `Show()`하고, 확인 콜백에서 카운트다운 재생
- **트러블슈팅 다수**:
  - `PreGameDescriptionUI.Awake()`에서 `root.SetActive(false)`를 호출해, 비활성 상태로 시작한 UI가 `Show()`로 처음 활성화되는 순간 `Awake`가 뒤늦게 실행되며 스스로 다시 꺼버리는 버그 → `Awake`에서 비활성화 로직 제거
  - 설명 UI가 뜬 동안 커서가 안 보여 확인 버튼을 클릭할 수 없던 문제 → 처음엔 `Cursor.lockState/visible` 직접 제어로 해결 시도했으나, 근본 원인은 `Throw`/`CameraSwitch` 액션이 둘 다 좌클릭에 바인딩돼 있어 확인 버튼 클릭이 `CameraRig.ToggleCamera()`도 함께 발동시켜 관전 카메라로 튕기던 것 → `PreGameDescriptionUI`가 `PlayerController.SetControlsLocked(true/false)`를 직접 걸고 풀어, 기존 "조작 잠금 중 좌클릭 무시" 가드를 재사용하도록 수정
  - ESC로 일시정지를 열었다 닫으면 설명 UI가 떠 있는데도 조작 잠금이 풀려버리는 문제 → `MenuEscapeBridge.ApplyResume()`에 `PreGameDescriptionUI.IsVisible` 체크 추가, 사망 팝업(`DeathPopup`)이 떠 있는 경우도 동일하게 가드 추가
  - 설명 UI~카운트다운 구간에 ESC를 열었다 닫으면 카운트다운 종료 후에도 테트리스 낙하가 계속 멈춰있는 문제 → 원인은 `MenuEscapeBridge.ApplyResume()`이 "카운트다운 중"이면 `SetTetrisGameplayPaused(false)` 호출을 건너뛰는데, 카운트다운이 실제로 끝나는 시점에 이를 다시 풀어주는 코드가 없었던 것 → `TetrisGameManager.StartSequenceForNewGame()`/`RestartFromDeath()`의 카운트다운 완료 콜백에서 `SetTetrisGameplayPaused(false)`를 명시적으로 재호출하도록 수정
- 테트리스 사망 재시작(`RestartFromDeath`)도 팩맨 재시작과 동일하게 페이드 아웃/인을 거치도록 변경 (기존엔 페이드 없이 즉시 재시작되어 팩맨과 불일치했음)

### 사망 팝업/UI 정리
- `DeathPopup` 배경을 `ui_3` 스프라이트로, 재시작 버튼 배경을 `Button` 스프라이트로 변경, "게임설명"/"허브로 나가기" 버튼 제거(재시작 버튼만 유지)
- 미사용 오브젝트 정리: `MainMenuUI`(IntroUI 포함, 실행 흐름상 켜질 방법이 없던 죽은 UI), `PausePopup`(코드에서 전혀 참조되지 않던 오브젝트), `DescriptionPopup`(원본, `GameSelectUI` 컴포넌트가 씬 어디에도 없어 고아 상태) 모두 확인 후 삭제 — 연쇄로 `MiniGameFlowManager.gameSelectUIRoot`/`ShowGameSelect()`, `SharedGameplayManager.gameSelectUI`/`OnClickShowDescription()`, `GameSelectUI.cs` 스크립트까지 함께 정리
- `GameFlow.Instance.ReportCurrent(...)`/`ReturnToMain()` 연동 상태 재확인 — `SharedGameplayManager.OnClickExitToHub()`에 이미 정확히 구현되어 있고 `RewardResultPopup`의 "메인 허브로" 버튼과도 정상 연결됨을 확인

### 설명 UI 콘텐츠 제작
- `TetrisDescriptionPopup`: 배경 `ui_3`, WASD 아이콘+"이동" / space 아이콘+"점프" / space+"+"+space 아이콘+"올라가기" 3행 구성. IconGroup 폭(260px)을 모든 행에서 통일해 아이콘 열/텍스트 열이 세로로 정렬되도록 배치, 텍스트는 전부 검은색
- `PacmanDescriptionPopup`: 배경 `ui_3`, WASD+"이동" / mouseLeftClick+"던지기" 2행 + 안내 문구("콩을 먹고 무당벌레가 빛날 때 던져야 죽일 수 있습니다!") 텍스트 행
- 두 팝업의 확인 버튼(`Button_Confirm`) 배경도 `Button` 스프라이트로 통일

### 팩맨 아트 텍스처 적용
- 사용자가 varco3d image로 시임리스 나뭇잎덤불 텍스처를 여러 차례 시안 제작 — "가장자리까지 꽉 채워 타일링되게" 프롬프트 반복 조정(no background/border/vignette, edge-to-edge fill 등 명시)을 거쳐 최종안(`Green Bush Texture`) 확정
- 팩맨 벽 전체(`Mat_PacmanWall_Pastel`)에 적용. 이후 미로를 감싸는 바깥 테두리 벽 7개(길이 18/21/45유닛으로 제각각)는 같은 텍스처를 쓰되 Tiling이 안 맞는 문제가 있어, 길이별로 전용 머티리얼 3종(`Mat_PacmanWall_Border_L18/L21/L45`)을 분리 생성해 각각 연결
- 천장: 기존에 전용 머티리얼 없이 URP 기본 `Lit.mat`을 그대로 쓰고 있던 것을 발견 → `Mat_PacmanCeiling_Pastel` 신규 생성, 벽과 동일한 Green Bush 텍스처 적용(Tiling 3,3)
- 바닥: 기존 단색 파스텔 크림색에서 갈색 흙길 톤(0.55, 0.4, 0.28)으로 변경 — 벽/천장이 전부 잎으로 덮이면서 바닥까지 같으면 공간 구분이 안 될 것을 우려해, 바닥만 다른 색으로 분리하기로 결정

### 기타
- `PowerPellet`/`ThrownPellet`/`HeldPelletVisual` 프리팹의 메시를 `pea_ammo` 모델로 교체, 메시-콜라이더 비율 문제로 메시를 별도 자식 오브젝트로 분리(콜라이더는 루트 유지)
- 바닥에 놓인 `PowerPellet`(손에 들거나 던질 때는 제외)에 Y축 자동 회전 추가
- `PlayerController.mouseSensitivity`를 다른 스크립트(옵션 UI 등)에서 참조할 수 있도록 `public`으로 변경(사용자 직접 작업)

## 14. 세션 6 종료 시점 남은 작업

- 사운드 작업 — 미착수
- 난이도 조절 — 미착수
- 블록 텍스쳐 — 미착수
- 테트리스 시퀀스 2개 추가(랜덤 구조 작성) — 미착수
- ESC 메뉴에 게임 설명 적기 — 미착수
- 목숨 하트 스프라이트 추가 — 미착수 (세션 3부터 임시 단색 원형 유지 중)
- `isDebugStartInPacman`, `isDebugStartAtTetrisClear` 디버그 옵션 — 최종 완성 전 반드시 해제
- 임시로 만들었던 `Mat_PacmanWall_Border_Pastel.mat`(길이 구분 없는 버전, 미사용) — 에디터에서 삭제 필요
- CLAUDE.md 위반사항 팀장 공유 — 아직 미해결
