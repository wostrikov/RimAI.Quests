# 快速开始指南

## 📋 前提条件

1. **RimWorld** 1.5或更高版本
2. **Harmony** 模组
3. **RimTalk** 模组（已配置API密钥）

## 🚀 5分钟快速开始

### 步骤1：设置开发环境

clone 到 RimWorld/Mods 目录下

```powershell
# 克隆或下载此仓库
cd RimTalk-Quests

# 运行环境设置脚本（仅需一次）
.\setup-env.ps1
```

> 你可能需要改下`setup-env.ps1`里的路径

### 步骤2：构建模组

```powershell
# 使用构建脚本
.\build.ps1

# 或指定配置
.\build.ps1 -GameVersion 1.6 -Configuration Release
```

### 步骤3：在RimWorld中启用

1. 启动RimWorld
2. 进入**模组**菜单
3. 确保加载顺序：
   ```
   ☑ Harmony
   ☑ Core
   ☑ RimTalk
   ☑ RimTalk - Quests  ← 新模组
   ```
4. 重启游戏

### 步骤4：配置（可选）

大部分配置都是直接从 RImTalk 的 Mod 配置页取的，这个 mod 本身的配置不多也没啥可改的

开始新游戏或加载存档，当任务生成时，你会看到AI生成的描述！
