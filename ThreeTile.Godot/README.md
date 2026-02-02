ThreeTile.Godot/              ← 🎮 Godot 外壳（只做展示 & 交互）
│   ├─ project.godot
│   ├─ ThreeTile.Godot.csproj    ← Godot 自动生成
│   │
│   ├─ Scenes/                   ← 场景（Node 树）
│   │   ├─ Main/
│   │   │   ├─ Main.tscn
│   │   │   └─ Main.cs
│   │   ├─ Game/
│   │   │   ├─ Game.tscn
│   │   │   └─ Game.cs
│   │   └─ UI/
│   │       ├─ HUD.tscn
│   │       └─ HUD.cs
│   │
│   ├─ Scripts/                  ← ❗纯逻辑脚本（不绑定具体 Scene）
│   │   ├─ Controllers/
│   │   │   └─ GameController.cs
│   │   ├─ ViewModels/
│   │   │   └─ LevelViewModel.cs
│   │   └─ Adapters/
│   │       └─ CoreToGodotAdapter.cs
│   │
│   ├─ Assets/                   ← 资源（贴图 / 音效 / 字体）
│   │   ├─ Sprites/
│   │   ├─ Audio/
│   │   └─ Fonts/
│   │
│   ├─ Prefabs/                  ← 可复用节点（Tile / Slot 等）
│   │   ├─ Tile.tscn
│   │   └─ Slot.tscn
│   │
│   └─ Bootstrap.cs              ← 启动 & 注入 Core
