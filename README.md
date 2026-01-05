# PlatformerKit

Unity 6 기반 2D 플랫포머 게임 개발 키트입니다. 확장 가능하고 모듈화된 플레이어 이동 시스템을 제공합니다.

## 주요 기능

### 현재 구현된 기능

- **기본 이동**: 좌우 이동, 가속/감속 없이 즉각적인 반응
- **점프 시스템**: 일반 점프 및 가변 점프 (점프 키를 떼면 낙하 속도 증가)
- **아래 점프**: 아래 방향 키를 누른 상태에서 점프 시 일방향 플랫폼을 통과
- **대시**: 좌우 방향으로 빠른 이동, 입력 방향 또는 모델 방향 기준
- **일방향 플랫폼**: 아래에서 위로만 통과 가능한 플랫폼

### 계획된 기능

- 벽 매달리기 및 벽 점프
- 벽 미끄러지기
- 난간 매달리기 및 난간 오르기
- 사다리 이동
- 이동 상태 머신 리팩토링

자세한 작업 목록은 [ToDo.md](./Documentation/ToDo.md)를 참조하세요.

## 요구사항

- **Unity 버전**: 6000.0.58f2 이상 (Unity 6)
- **렌더 파이프라인**: Universal Render Pipeline (URP)
- **필수 패키지**:
  - Input System (1.14.2)
  - 2D Feature Set (2.0.1)
  - Universal RP (17.0.4)

## 프로젝트 구조

```
PlatformerKit/
├── Assets/
│   ├── Scripts/
│   │   ├── Runtime/
│   │   │   ├── Player/
│   │   │   │   ├── PlayerController.cs    # 입력 처리 및 로직 조율
│   │   │   │   └── PlayerPhysics.cs       # 물리 처리 및 이동 로직
│   │   │   ├── Environment/
│   │   │   │   ├── OneWayPlatform.cs      # 일방향 플랫폼
│   │   │   │   └── CollisionSetup.cs      # 충돌 레이어 설정
│   │   │   └── Debug/
│   │   │       ├── DebugLogger.cs
│   │   │       └── PlayerGroundedDebugger.cs
│   │   └── Editor/
│   ├── Scenes/
│   │   ├── GameMain.unity
│   │   └── SampleScene.unity
│   └── InputSystem_Actions.inputactions
└── Documentation/
    ├── PlayerMovement.md                    # 플레이어 이동 시스템 인덱스
    ├── PlayerMovement-Design.md             # 설계 원칙 및 아키텍처
    ├── PlayerMovement-Implementation.md     # 구현 세부사항 및 사용법
    ├── PlayerMovement-Architecture.md       # 클래스 분리 및 상태 머신 설계
    ├── ToDo.md                              # 작업 목록
    └── TeamDocsStyleGuide.md                # 문서 작성 가이드
```

## 빠른 시작

### 1. 프로젝트 열기

1. Unity Hub에서 Unity 6 버전 설치
2. 이 프로젝트를 Unity Hub에 추가
3. 프로젝트 열기

### 2. 씬 실행

1. `Assets/Scenes/GameMain.unity` 또는 `SampleScene.unity` 열기
2. Play 버튼 클릭

### 3. 조작 방법

- **좌우 이동**: `A` / `D` 또는 `←` / `→`
- **점프**: `Space`
- **대시**: `Left Shift`
- **아래 점프**: `S` + `Space` (일방향 플랫폼 위에서)

## 문서

프로젝트의 상세한 문서는 `Documentation/` 폴더에 있습니다.

### 플레이어 이동 시스템

- **[PlayerMovement.md](./Documentation/PlayerMovement.md)**: 플레이어 이동 시스템 문서 인덱스
- **[PlayerMovement-Design.md](./Documentation/PlayerMovement-Design.md)**: 설계 원칙, 아키텍처, 클래스 구조
- **[PlayerMovement-Implementation.md](./Documentation/PlayerMovement-Implementation.md)**: 구현 세부사항, API 사용법, 설정 방법
- **[PlayerMovement-Architecture.md](./Documentation/PlayerMovement-Architecture.md)**: 클래스 분리 구조 및 상태 머신 설계

### 기타 문서

- **[ToDo.md](./Documentation/ToDo.md)**: 향후 작업 목록 및 기능 계획
- **[TeamDocsStyleGuide.md](./Documentation/TeamDocsStyleGuide.md)**: 문서 작성 규칙 및 스타일 가이드

## 개발 가이드

### 코드 스타일

프로젝트는 Unity C# 표준 및 모범 사례를 따릅니다:

- **클래스/파일 이름**: PascalCase (예: `PlayerController.cs`)
- **private 필드**: `_camelCase` (예: `_playerHp`)
- **프로퍼티**: PascalCase (예: `IsGrounded`)
- **메서드**: PascalCase, 동사로 시작 (예: `ApplyDamage()`)
- **주석**: 한국어로 작성
- **Unity 이벤트**: 클래스 내에서 그룹화하여 선언

자세한 코드 스타일 가이드는 프로젝트 내 규칙을 참조하세요.

### 아키텍처 원칙

- **관심사 분리**: 입력 처리, 로직 조율, 물리 처리를 분리
- **모듈화**: 각 기능을 독립적으로 확장 가능하도록 설계
- **확장성**: 새로운 이동 기능 추가 시 기존 코드 수정 최소화

### PlayerController와 PlayerPhysics

현재 구조는 다음과 같이 분리되어 있습니다:

- **PlayerController**: 입력 처리 및 로직 조율
- **PlayerPhysics**: 물리 처리 및 이동 로직 실행

향후 상태 머신 도입을 통해 더욱 체계적인 구조로 개선할 예정입니다.

## 주요 컴포넌트

### PlayerController

플레이어 입력을 처리하고 `PlayerPhysics`에 이동 요청을 전달합니다.

```csharp
// 주요 기능
- 수평 입력 처리
- 점프 입력 감지 (일반/아래 점프)
- 대시 입력 처리
- 모델 방향 업데이트
```

### PlayerPhysics

물리 기반 이동 로직을 처리합니다.

```csharp
// 주요 기능
- Rigidbody2D 속도 제어
- 충돌 감지 (다중 Raycast 기반: 지면, 천장, 좌우 벽)
- 점프 처리 (일반/가변/아래 점프)
- 대시 처리
- 중력 및 물리 설정 관리
```

### OneWayPlatform

일방향 플랫폼 기능을 제공합니다. 아래에서 위로만 통과 가능하며, 위에서 아래로는 통과하지 않습니다.

## 기여

프로젝트에 기여하기 전에 다음을 확인하세요:

1. [TeamDocsStyleGuide.md](./Documentation/TeamDocsStyleGuide.md)의 문서 작성 규칙 준수
2. 코드 스타일 가이드 준수
3. 변경 사항에 대한 문서 업데이트

## 라이선스

이 프로젝트의 라이선스 정보는 별도로 명시되지 않았습니다. 사용 전 확인이 필요합니다.

## 문의

프로젝트 관련 문의사항이나 버그 리포트는 이슈 트래커를 통해 제출해 주세요.

