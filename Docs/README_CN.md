# RimTalk-Quests

> **为RimWorld提供动态AI驱动的任务描述生成**

一个RimWorld模组，扩展了[RimTalk](https://github.com/juicycleff/RimTalk)，使用大语言模型生成有上下文感知的AI驱动任务描述。

## 📋 关于

RimTalk-Quests拦截RimWorld的任务生成系统，将静态任务描述替换为由AI生成的动态、叙事丰富的内容。通过复用RimTalk的模型配置和API基础设施，此模组创建了能够适应你殖民地情况的独特任务叙述。

### 归属声明

本模组是基于[juicy](https://github.com/juicycleff/RimTalk)的**RimTalk**的**衍生作品**。  
采用[CC BY-NC-SA 4.0 国际许可协议](http://creativecommons.org/licenses/by-nc-sa/4.0/)授权。

## ✨ 功能特性

- 🎯 **AI生成的任务描述** - 每个任务都具有独特且符合上下文的叙述
- 🔄 **无缝集成** - 使用你现有的RimTalk AI配置
- 🎮 **上下文感知** - 描述根据殖民地状态、派系关系和任务历史自适应
- 🌐 **多模型支持** - 支持Google Gemini、OpenAI兼容API等RimTalk支持的所有模型
- ⚡ **实时流式生成** - 观看任务描述实时流式生成
- 🎨 **富文本保留** - 保持RimWorld的颜色标签和格式化
- 🚀 **无反射** - 仅使用公开的RimTalk API实现，架构清晰

## 📦 需求

### 依赖项

1. **[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)** - 补丁所需
2. **[RimTalk](https://steamcommunity.com/sharedfiles/filedetails/?id=3378714281)** - 必须安装并配置API密钥

### 兼容性

- RimWorld 1.5+
- RimWorld 1.6+（已测试）

## 🚀 安装

1. 从Steam创意工坊或GitHub安装**Harmony**
2. 安装**RimTalk**并配置你的AI API密钥
3. 安装**RimTalk-Quests**（本模组）
4. 确保加载顺序：`Harmony → RimTalk → RimTalk-Quests`

## ⚙️ 配置

RimTalk-Quests使用你现有的RimTalk设置：
- **API密钥**：在RimTalk设置中配置
- **模型选择**：使用你的RimTalk模型选择（Gemini、OpenAI等）
- **速率限制**：遵守RimTalk的API速率限制

无需额外配置！

### 模组设置

在游戏内的模组设置中，你可以：
- ✅ **启用/禁用AI任务描述** - 开关AI生成的任务描述功能
- 🐛 **详细调试日志** - 启用逐块流式传输的详细日志，用于故障排除
- 📊 **处理状态** - 查看当前正在处理的任务数量

## 🛠️ 开发

### 从源码构建

```powershell
# 设置RimWorld安装路径（首次运行）
.\setup-env.ps1

# 构建项目
.\build.ps1

# 或者指定版本
.\build.ps1 -GameVersion 1.6 -Configuration Debug
```

### 手动构建

```powershell
# 设置环境变量
$env:RIMWORLD_DIR = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld"

# 构建
dotnet build RimTalkQuests.csproj /p:GameVersion=1.6
```

### 项目结构

```
RimTalk-Quests/
├── About/                    # 模组元数据
│   └── About.xml
├── Source/                   # C#源代码
│   ├── RimTalkQuestsMod.cs              # 主模组类
│   ├── Patches/                          # Harmony补丁
│   │   └── QuestPatches.cs
│   └── Services/                         # 核心服务
│       └── QuestDescriptionGenerator.cs
├── 1.5/Assemblies/           # RimWorld 1.5编译的DLL
├── 1.6/Assemblies/           # RimWorld 1.6编译的DLL
├── build.ps1                 # 构建脚本
├── setup-env.ps1             # 环境设置脚本
└── LICENSE                   # CC BY-NC-SA 4.0许可证
```

### 技术实现

#### 工作原理

1. **任务拦截**：Harmony补丁拦截`Quest.PostAdded()`方法（任务首次添加时）
2. **上下文构建**：提取任务信息、殖民地状态、派系关系和历史任务记录
3. **流式AI调用**：使用自定义的`PlainTextStreamingClient`直接构造HTTP请求
4. **实时更新**：每收到一个文本块就更新`quest.description`字段
5. **富文本保留**：在AI提示中要求保留原始的`<color>`标签

#### 关键特性

- **异步流式生成**：实时显示AI生成过程，用户体验更流畅
- **无反射架构**：完全使用RimTalk的公开API和数据类型
- **直接HTTP请求**：绕过RimTalk的`JsonStreamParser`bug，直接处理SSE流
- **优雅降级**：如果AI服务不可用，保留原始描述
- **分离式实现**：OpenAI和Gemini逻辑分开维护，易于扩展

### 开发路线图

- [x] 基础项目结构和Harmony集成
- [x] RimTalk AI服务集成（流式支持）
- [x] 任务描述生成的上下文构建器（场景+派系历史）
- [x] 提示工程（保留富文本标签）
- [x] 实时流式生成和块回调
- [x] 设置UI
- [x] 富文本（颜色标签）保留
- [x] 无反射实现，仅使用公开API
- [ ] 性能优化和缓存系统
- [ ] 本地化支持
- [ ] 任务名称AI生成（可选）

## 🤝 贡献

欢迎贡献！请注意，此项目继承了RimTalk的CC BY-NC-SA 4.0许可证。

### 指南

- 保持代码质量和文档
- 测试多种任务类型
- 遵守API速率限制
- 遵循RimWorld模组开发最佳实践

## 📄 许可证

本作品采用**知识共享 署名-非商业性使用-相同方式共享 4.0 国际许可协议**授权。

- **署名（Attribution）**：基于juicy的RimTalk
- **非商业性使用（NonCommercial）**：不得用于商业用途
- **相同方式共享（ShareAlike）**：衍生作品必须使用相同许可证

详见[LICENSE](LICENSE)文件。

### 第三方许可证

- **RimTalk**：CC BY-NC-SA 4.0 by [juicy](https://github.com/juicycleff/RimTalk)
- **Harmony**：MIT License by [Andreas Pardeike](https://github.com/pardeike/Harmony)
- **RimWorld**：Ludeon Studios专有许可证

## 🐛 已知问题

- 首次运行时需要配置RimTalk的API密钥
- 任务描述在任务出现时生成（某些情况下不会立即可见）
- 尚未实现性能优化和缓存系统
- 大量任务可能影响API速率限制

## 📞 支持

- **问题反馈**：[GitHub Issues](https://github.com/yourusername/RimTalk-Quests/issues)
- **原始RimTalk**：[RimTalk仓库](https://github.com/juicycleff/RimTalk)

## 🙏 致谢

- **juicy** - RimTalk的创建者，本模组基于其工作
- **Andreas Pardeike** - Harmony的创建者
- **Ludeon Studios** - RimWorld
- **RimWorld模组社区** - 文档和支持

## 📝 更新日志

### v1.0.0（初始版本）
- ✨ 通过RimTalk实现AI驱动的任务描述
- 🎯 任务生成的Harmony补丁
- 💾 描述缓存系统
- ⚙️ 游戏内设置UI
- 🔄 与RimTalk AI服务的集成（通过反射）

---

**注意**：此模组需要有效的互联网连接和有效的API密钥（通过RimTalk配置）才能生成任务描述。
