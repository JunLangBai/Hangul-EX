# ===================================================================================
# Git 仓库智能分析与 LFS 优化工具（功能完整版 + 扩展名详情 + LFS建议分组）
#
# 功能汇总:
#   - 扩展名类型与中文描述映射表，通过 -ShowExtensionDetails 开关显示
#   - 扫描 git 追踪文件并收集详细信息
#   - 支持忽略特定扩展 (-IgnoreExtensions)
#   - 多文件抽样二进制检测 (-SampleCount)
#   - 限制未 LFS 样本显示 TopM (-TopMSamples)
#   - LFS 建议：按总占用降序，排除平均过小扩展
#   - 读取 .gitattributes、core.quotepath 暂时修改与恢复、警告大文本文件等
#   - 输出 TopN、目录统计、LFS 覆盖率、仓库估算大小
#   - 每个 Top 文件显示 LFS 状态 (✅/❌)
#   - 导出报告支持 Markdown (-ExportReport "<path>")
# ===================================================================================

param(
    [string]$Path = ".",
    [long]$SizeThreshold = 500KB,
    [int]$TopNFiles = 40,
    [int]$DirectoryDepth = 2,
    [int]$SampleCount = 3,
    [int]$TopMSamples = 50,
    [string[]]$IgnoreExtensions = @(),
    [string[]]$KnownTextExtensions = @(
        # ----- 通用源代码与脚本 -----
        ".c", ".cpp", ".h", ".hpp",         # C/C++
        ".cs",                              # C#
        ".java", ".kt", ".groovy", ".scala", # JVM 语言
        ".js", ".ts", ".mjs", ".cjs",       # JavaScript/TypeScript
        ".py", ".pyi",                      # Python
        ".go",                              # Go
        ".rs",                              # Rust
        ".rb",                              # Ruby
        ".php",                             # PHP
        ".swift",                           # Swift
        ".lua",                             # Lua
        ".pl", ".pm",                       # Perl
        ".r",                               # R
        ".sh", ".bash", ".zsh",             # Shell 脚本
        ".ps1", ".psm1", ".psd1",            # PowerShell 脚本
        ".bat", ".cmd",                     # Windows 批处理

        # ----- Web 开发 -----
        ".html", ".htm",                    # HTML
        ".css", ".scss", ".sass", ".less",  # 样式表
        ".jsx", ".tsx",                     # React/JSX
        ".vue",                             # Vue.js 单文件组件
        ".svelte",                          # Svelte 组件
        ".graphql", ".gql",                 # GraphQL 查询语言

        # ----- 数据与配置 -----
        ".json", ".jsonc",                  # JSON
        ".xml", ".xsd", ".xsl", ".xslt",     # XML 相关
        ".yml", ".yaml",                    # YAML
        ".ini", ".cfg", ".conf",            # INI/Config
        ".toml",                            # TOML
        ".properties",                      # Java Properties
        ".env",                             # 环境变量文件

        # ----- 文档与标记语言 -----
        ".txt", ".md", ".markdown",         # 纯文本 & Markdown
        ".rtf",                             # 富文本
        ".tex",                             # LaTeX
        ".csv", ".tsv",                     # 表格数据
        ".log",                             # 日志文件
        ".sql",                             # SQL 脚本

        # ----- 项目、构建与依赖 -----
        ".sln", ".csproj", ".vbproj", ".fsproj", ".vcxproj", # .NET/Visual Studio
        ".suo", ".user",                    # VS 用户配置
        ".config", ".settings", ".resx",    # .NET 配置
        "pom.xml",                          # Maven
        ".gradle", ".kts",                  # Gradle
        "package.json", "package-lock.json", "yarn.lock", # Node.js
        "Pipfile", "Pipfile.lock", "requirements.txt", # Python
        "composer.json", "composer.lock",  # PHP Composer
        "Gemfile", "Gemfile.lock",          # Ruby Bundler
        "CMakeLists.txt", ".cmake",         # CMake
        ".mak", "Makefile",                 # Make
        ".xcodeproj", ".pbxproj",           # Xcode
        ".plist",                           # Apple Property List
        "AndroidManifest.xml",              # Android Manifest
        
        # ----- DevOps -----
        "Dockerfile", ".dockerignore",      # Docker
        ".tf", ".tfvars", ".hcl",           # Terraform / HCL
        
        # ----- 版本控制 -----
        ".gitignore", ".gitattributes", ".gitmodules", ".gitconfig",
        
        # ----- 着色器 -----
        ".shader", ".cginc", ".hlsl", ".glsl",
        ".shadergraph", ".shadersubgraph",

        # ----- 游戏引擎 (Unity) -----
        ".unity", ".prefab", ".asset", ".mat", ".meta", ".playable",
        ".controller", ".anim", ".guiskin", ".uxml", ".uss", ".asmdef"
    ),
    [switch]$NoDirectoryAnalysis,
    [switch]$NoLfsSuggestions,
    [switch]$ShowExtensionDetails,
    [string]$ExportReport = ""
)

$ErrorActionPreference = "Stop"

# ---------------------------
# 扩展名 -> 类型 映射表
# ---------------------------
$extensionTypeMap = @{
    # ----- 图像 -----
    '.png' = '图片'; '.jpg' = '图片'; '.jpeg' = '图片'; '.gif' = '图片'; '.bmp' = '图片';
    '.tga' = '图片'; '.psd' = '图片(源)'; '.tif' = '图片'; '.tiff' = '图片'; '.ico' = '图标';
    '.webp' = '图片'; '.heic' = '图片'; '.exr' = '图片(HDR)'; '.hdr' = '图片(HDR)';
    '.svg' = '矢量图形'; '.ai' = '矢量图形(源)'; '.eps' = '矢量图形';
    '.sketch' = '设计稿'; '.xd' = '设计稿';

    # ----- 音频 -----
    '.wav' = '音频'; '.mp3' = '音频'; '.ogg' = '音频'; '.flac' = '音频'; '.aiff' = '音频';
    '.m4a' = '音频'; '.wma' = '音频'; '.aac' = '音频';

    # ----- 视频 -----
    '.mp4' = '视频'; '.mov' = '视频'; '.avi' = '视频'; '.webm' = '视频'; '.mkv' = '视频';
    '.wmv' = '视频'; '.flv' = '视频'; '.mpg' = '视频'; '.mpeg' = '视频';

    # ----- 3D 模型与场景 -----
    '.fbx' = '模型'; '.obj' = '模型'; '.blend' = '模型(源)'; '.max' = '模型(源)'; '.c4d' = '模型(源)';
    '.gltf' = '模型'; '.glb' = '模型(二进制)'; '.dae' = '模型'; '.stl' = '模型(3D打印)'; '.usdz' = 'AR模型';

    # ----- 文档与办公 -----
    '.pdf' = '文档'; '.doc' = '文档(Word)'; '.docx' = '文档(Word)'; '.xls' = '表格(Excel)'; '.xlsx' = '表格(Excel)';
    '.ppt' = '演示(PPT)'; '.pptx' = '演示(PPT)'; '.rtf' = '富文本'; '.chm' = '帮助文档';
    '.epub' = '电子书'; '.mobi' = '电子书';

    # ----- 字体 -----
    '.ttf' = '字体'; '.otf' = '字体'; '.woff' = '字体'; '.woff2' = '字体';

    # ----- 压缩包与磁盘镜像 -----
    '.zip' = '压缩包'; '.rar' = '压缩包'; '.7z' = '压缩包'; '.tar' = '压缩包'; '.gz' = '压缩包';
    '.bz2' = '压缩包'; '.xz' = '压缩包'; '.iso' = '磁盘镜像'; '.dmg' = '磁盘镜像(macOS)';

    # ----- 库、可执行文件与编译产物 -----
    '.exe' = '可执行'; '.dll' = '库(.NET)'; '.pdb' = '调试符号(.NET)'; '.nupkg' = '包(NuGet)';
    '.jar' = '库(Java)'; '.war' = 'Web包(Java)'; '.ear' = '企业包(Java)'; '.class' = 'Java字节码';
    '.so' = '库(Linux)'; '.a' = '库(Unix)'; '.lib' = '库(Windows)'; '.dylib' = '库(macOS)';
    '.bundle' = '包(macOS)'; '.apk' = '包(Android)'; '.aab' = '包(Android)'; '.ipa' = '包(iOS)';
    '.pyc' = 'Python字节码'; '.whl' = '包(Python)'; '.deb' = '包(Debian)'; '.rpm' = '包(RedHat)';
    
    # ----- 数据库与数据文件 -----
    '.db' = '数据库'; '.sqlite' = '数据库'; '.sqlite3' = '数据库'; '.mdb' = '数据库'; '.accdb' = '数据库';
    '.sql' = 'SQL脚本'; '.bak' = '数据库备份'; '.dump' = '数据转储';
    '.pkl' = '数据(Pickle)'; '.h5' = '数据(HDF5)';

    # ----- 证书与密钥 -----
    '.cer' = '证书'; '.crt' = '证书'; '.pem' = '证书'; '.key' = '私钥';
    '.pfx' = '证书交换'; '.p12' = '证书交换'; '.jks' = '密钥库(Java)';

    # ----- 源代码与脚本 (类型分类) -----
    '.cs' = 'C#源码'; '.java' = 'Java源码'; '.kt' = 'Kotlin源码'; '.swift' = 'Swift源码';
    '.c' = 'C源码'; '.cpp' = 'C++源码'; '.h' = 'C/C++头文件'; '.hpp' = 'C++头文件';
    '.js' = 'JS脚本'; '.ts' = 'TS脚本'; '.jsx' = 'React组件'; '.tsx' = 'React组件'; '.vue' = 'Vue组件';
    '.py' = 'Python脚本'; '.go' = 'Go源码'; '.rs' = 'Rust源码'; '.rb' = 'Ruby脚本';
    '.sh' = 'Shell脚本'; '.ps1' = 'PowerShell脚本'; '.bat' = '批处理脚本';
    '.shader' = '着色器'; '.cginc' = '着色器'; '.glsl' = '着色器'; '.hlsl' = '着色器';
    '.html' = '网页'; '.css' = '样式表'; '.scss' = '样式表';

    # ----- 配置与项目文件 (类型分类) -----
    '.json' = '配置/数据'; '.xml' = '配置/数据'; '.yml' = '配置'; '.yaml' = '配置'; '.ini' = '配置'; '.toml' = '配置';
    '.sln' = 'VS解决方案'; '.csproj' = 'C#项目'; '.vcxproj' = 'C++项目'; 'pom.xml' = 'Maven项目'; '.gradle' = 'Gradle脚本';
    'Dockerfile' = '容器配置'; '.tf' = 'IaC脚本'; '.ipynb' = 'Notebook';
    
    # ----- Unity 特定 -----
    '.unity' = 'Unity场景'; '.prefab' = 'Unity预制体'; '.asset' = 'Unity资源'; '.mat' = 'Unity材质';
    '.meta' = 'Unity元数据'; '.anim' = 'Unity动画'; '.controller' = 'Unity动画控制器'; '.unitypackage' = 'Unity包';

    # ----- 其他 -----
    '.bytes' = '二进制数据'; '.sfx' = '自解压包'; '.log' = '日志'; '.md' = '文档'; '.txt' = '文本';
    '.gitignore' = 'Git配置'; '.gitattributes' = 'Git配置'
}

# ---------------------------
# 扩展名 -> 名称 映射表
# ---------------------------
$extensionNameMap = @{
    # ----- 图像 -----
    '.png' = '便携式网络图形 (PNG)'; '.jpg' = 'JPEG 图像'; '.jpeg' = 'JPEG 图像'; '.gif' = '动态图像 (GIF)'; '.bmp' = '位图图像';
    '.tga' = 'Targa 图像'; '.psd' = 'Photoshop 设计稿'; '.tif' = '标签图像文件 (TIFF)'; '.tiff' = '标签图像文件 (TIFF)'; '.ico' = '图标文件';
    '.webp' = 'WebP 图像'; '.heic' = '高效图像文件格式 (HEIC)'; '.exr' = '高动态范围图像 (OpenEXR)'; '.hdr' = '高动态范围光照贴图';
    '.svg' = '可缩放矢量图形 (SVG)'; '.ai' = 'Adobe Illustrator 文件'; '.eps' = '封装式 PostScript';
    '.sketch' = 'Sketch 设计文件'; '.xd' = 'Adobe XD 设计文件';

    # ----- 音频 -----
    '.wav' = '波形音频'; '.mp3' = 'MPEG 音频层 III'; '.ogg' = 'Ogg Vorbis 音频'; '.flac' = '无损音频编码'; '.aiff' = '音频交换文件格式';
    '.m4a' = 'MPEG-4 音频'; '.wma' = 'Windows Media 音频'; '.aac' = '高级音频编码';

    # ----- 视频 -----
    '.mp4' = 'MPEG-4 视频'; '.mov' = 'Apple QuickTime 视频'; '.avi' = '音视频交错格式'; '.webm' = 'WebM 视频'; '.mkv' = 'Matroska 视频容器';
    '.wmv' = 'Windows Media 视频'; '.flv' = 'Flash 视频'; '.mpg' = 'MPEG 视频'; '.mpeg' = 'MPEG 视频';

    # ----- 3D 模型与场景 -----
    '.fbx' = 'Autodesk FBX 模型'; '.obj' = 'Wavefront 3D 对象'; '.blend' = 'Blender 源文件'; '.max' = '3ds Max 源文件'; '.c4d' = 'Cinema 4D 源文件';
    '.gltf' = 'GL 传输格式 (文本)'; '.glb' = 'GL 传输格式 (二进制)'; '.dae' = 'Collada 数字资产交换'; '.stl' = '立体光刻文件 (3D打印)'; '.usdz' = '通用场景描述 (AR)';

    # ----- 文档与办公 -----
    '.pdf' = 'PDF 文档'; '.doc' = 'Microsoft Word 文档 (旧)'; '.docx' = 'Microsoft Word 文档'; '.xls' = 'Microsoft Excel 表格 (旧)'; '.xlsx' = 'Microsoft Excel 表格';
    '.ppt' = 'Microsoft PowerPoint 演示文稿 (旧)'; '.pptx' = 'Microsoft PowerPoint 演示文稿'; '.rtf' = '富文本格式文档'; '.chm' = '编译的 HTML 帮助文件';
    '.epub' = 'EPUB 电子书'; '.mobi' = 'Mobipocket 电子书';

    # ----- 字体 -----
    '.ttf' = 'TrueType 字体'; '.otf' = 'OpenType 字体'; '.woff' = 'Web 开放字体格式'; '.woff2' = 'Web 开放字体格式 2.0';

    # ----- 压缩包与磁盘镜像 -----
    '.zip' = 'ZIP 压缩文件'; '.rar' = 'RAR 压缩文件'; '.7z' = '7-Zip 压缩文件'; '.tar' = '磁带归档文件'; '.gz' = 'Gzip 压缩文件';
    '.bz2' = 'Bzip2 压缩文件'; '.xz' = 'XZ 压缩文件'; '.iso' = 'ISO-9660 磁盘镜像'; '.dmg' = 'Apple 磁盘镜像';

    # ----- 库、可执行文件与编译产物 -----
    '.exe' = 'Windows 可执行文件'; '.dll' = '动态链接库 (.NET)'; '.pdb' = '程序数据库 (调试符号)'; '.nupkg' = 'NuGet 包';
    '.jar' = 'Java Archive'; '.war' = 'Web Application Archive (Java)'; '.ear' = 'Enterprise Application Archive (Java)'; '.class' = 'Java 编译字节码';
    '.so' = '共享对象库 (Linux/Unix)'; '.a' = '静态库 (Unix)'; '.lib' = '静态库 (Windows)'; '.dylib' = 'macOS 动态库';
    '.bundle' = 'macOS 应用包/插件'; '.apk' = 'Android 应用包'; '.aab' = 'Android App Bundle'; '.ipa' = 'iOS App Store 包';
    '.pyc' = '已编译的 Python 文件'; '.whl' = 'Python Wheel 包'; '.deb' = 'Debian 软件包'; '.rpm' = 'Red Hat 软件包管理器';
    
    # ----- 数据库与数据文件 -----
    '.db' = '通用数据库文件'; '.sqlite' = 'SQLite 数据库'; '.sqlite3' = 'SQLite 3 数据库'; '.mdb' = 'Microsoft Access 数据库 (旧)'; '.accdb' = 'Microsoft Access 数据库';
    '.sql' = 'SQL 脚本文件'; '.bak' = '数据库备份文件'; '.dump' = '数据库转储文件';
    '.pkl' = 'Python Pickle 数据'; '.h5' = 'HDF5 数据文件';

    # ----- 证书与密钥 -----
    '.cer' = '安全证书文件'; '.crt' = '安全证书文件'; '.pem' = '隐私增强邮件证书'; '.key' = '公钥/私钥文件';
    '.pfx' = '个人信息交换格式'; '.p12' = 'PKCS #12 证书包'; '.jks' = 'Java KeyStore';

    # ----- 源代码与脚本 -----
    '.cs' = 'C# 源代码'; '.java' = 'Java 源代码'; '.kt' = 'Kotlin 源代码'; '.swift' = 'Swift 源代码';
    '.c' = 'C 源代码'; '.cpp' = 'C++ 源代码'; '.h' = 'C/C++ 头文件'; '.hpp' = 'C++ 头文件';
    '.js' = 'JavaScript 脚本'; '.ts' = 'TypeScript 脚本'; '.jsx' = 'React JSX 组件'; '.tsx' = 'React TSX 组件'; '.vue' = 'Vue.js 单文件组件';
    '.py' = 'Python 脚本'; '.go' = 'Go 语言源代码'; '.rs' = 'Rust 源代码'; '.rb' = 'Ruby 脚本';
    '.sh' = 'Shell 脚本'; '.ps1' = 'PowerShell 脚本'; '.bat' = 'Windows 批处理文件';
    '.shader' = '着色器代码'; '.cginc' = 'Cg/HLSL 着色器头文件'; '.glsl' = 'OpenGL 着色语言'; '.hlsl' = '高级着色语言';
    '.html' = '超文本标记语言'; '.css' = '层叠样式表'; '.scss' = 'Sass 层叠样式表';

    # ----- 配置与项目文件 -----
    '.json' = 'JSON 数据/配置'; '.xml' = 'XML 数据/配置'; '.yml' = 'YAML 数据/配置'; '.yaml' = 'YAML 数据/配置'; '.ini' = '初始化配置文件'; '.toml' = 'Tom 的显式最小化语言配置';
    '.sln' = 'Visual Studio 解决方案'; '.csproj' = 'Visual Studio C# 项目'; '.vcxproj' = 'Visual Studio C++ 项目'; 'pom.xml' = 'Maven Project Object Model'; '.gradle' = 'Gradle 构建脚本';
    'Dockerfile' = 'Docker 容器定义文件'; '.tf' = 'Terraform 配置文件'; '.ipynb' = 'Jupyter Notebook';

    # ----- Unity 特定 -----
    '.unity' = 'Unity 场景文件'; '.prefab' = 'Unity 预制体'; '.asset' = 'Unity 可序列化资源'; '.mat' = 'Unity 材质';
    '.meta' = 'Unity 资源元数据'; '.anim' = 'Unity 动画剪辑'; '.controller' = 'Unity 动画控制器'; '.unitypackage' = 'Unity 资源包';

    # ----- 其他 -----
    '.bytes' = '原始二进制数据'; '.sfx' = '自解压可执行文件'; '.log' = '日志文件'; '.md' = 'Markdown 文档'; '.txt' = '纯文本文档';
    '.gitignore' = 'Git 忽略规则文件'; '.gitattributes' = 'Git 属性文件'
}


# ---------------------------
# 辅助函数
# ---------------------------

<#
.SYNOPSIS
将字节数格式化为易于人类阅读的字符串（如 KB, MB, GB）。
#>
function Format-Bytes {
    param([double]$bytes)
    if ($bytes -eq 0) { return "0 B" }
    if ($bytes -ge 1GB) { return ("{0:N2} GB" -f ($bytes / 1GB)) }
    if ($bytes -ge 1MB) { return ("{0:N2} MB" -f ($bytes / 1MB)) }
    if ($bytes -ge 1KB) { return ("{0:N2} KB" -f ($bytes / 1KB)) }
    return ("{0:N0} B" -f $bytes)
}

<#
.SYNOPSIS
通过检查文件头部是否包含空字节（0x00）来判断其是否为二进制文件。
#>
function Test-IsBinary {
    param([string]$FilePath)
    try {
        if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) { return $false }
        $bytes = Get-Content -LiteralPath $FilePath -Encoding Byte -TotalCount 1024 -ErrorAction SilentlyContinue
        if ($null -eq $bytes) { return $true } # 空文件或读取失败，倾向于认为是二进制
        return ($bytes -contains 0)
    }
    catch {
        return $true # 发生异常时，保守地认为是二进制
    }
}

<#
.SYNOPSIS
安全地将输入路径解析为单一的、绝对的路径字符串。
#>
function Get-AbsolutePath {
    param([string]$p)
    try {
        $rp = Resolve-Path -LiteralPath $p -ErrorAction Stop | Select-Object -First 1
        return $rp.Path
    }
    catch {
        throw ("无法解析路径: {0}" -f $p)
    }
}

<#
.SYNOPSIS
根据根路径计算文件的相对路径。
#>
function Get-RelativePath {
    param([string]$Root, [string]$Full)
    $rootNorm = $Root.TrimEnd('\', '/').Replace('/', [IO.Path]::DirectorySeparatorChar)
    $fullNorm = $Full.Replace('/', [IO.Path]::DirectorySeparatorChar)
    if ($fullNorm.StartsWith($rootNorm, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullNorm.Substring($rootNorm.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
    }
    else { return $fullNorm }
}

<#
.SYNOPSIS
使用 `git ls-files -z` 命令获取所有被 Git 跟踪的文件列表，以正确处理特殊字符。
#>
function Get-GitTrackedFiles {
    param([string]$repo)
    [System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $raw = git -C $repo ls-files -z --full-name 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $raw) { throw "git ls-files 失败或无输出" }
    return ($raw -split [char]0 | Where-Object { $_ -ne "" })
}

<#
.SYNOPSIS
使用 `git lfs ls-files` 命令获取所有被 Git LFS 跟踪的文件列表。
#>
function Get-GitLfsFiles {
    param([string]$repo)
    try {
        $raw = git -C $repo lfs ls-files 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $raw) { return @() }
        # 输出行格式: "<oid> <size> <path>"
        return ($raw | ForEach-Object {
                $parts = $_ -split '\s+'
                if ($parts.Count -ge 3) { $parts[2] } else { $null }
            } | Where-Object { $_ })
    }
    catch { return @() }
}

# ---------------------------
# 主流程：初始化与验证
# ---------------------------

try {
    $absoluteProjectPath = Get-AbsolutePath -p $Path
}
catch {
    Write-Host ("❌ 无法解析路径 '{0}'。" -f $Path) -ForegroundColor Red
    Write-Host $_.Exception.Message
    exit 1
}

Write-Host ("🚀 开始分析 Git 仓库: {0}" -f $absoluteProjectPath)

$gitDir = Join-Path -Path $absoluteProjectPath -ChildPath ".git"
if (-not (Test-Path -LiteralPath $gitDir)) {
    Write-Host ("❌ 路径 '{0}' 不是 Git 仓库根目录（未找到 .git）。" -f $absoluteProjectPath) -ForegroundColor Red
    exit 1
}

$originalQuotePath = $null
$quotePathIsSet = $false
$didChangeQuotePath = $false

try {
    try {
        $originalQuotePath = git -C $absoluteProjectPath config --get core.quotepath 2>$null
        if ($LASTEXITCODE -eq 0 -and $null -ne $originalQuotePath -and $originalQuotePath -ne "") {
            $quotePathIsSet = $true
        }
        else {
            $originalQuotePath = $null
            $quotePathIsSet = $false
        }
    }
    catch { $originalQuotePath = $null; $quotePathIsSet = $false }

    try {
        if ($originalQuotePath -ne "false") {
            git -C $absoluteProjectPath config core.quotepath false 2>$null
            if ($LASTEXITCODE -eq 0) { $didChangeQuotePath = $true; Write-Host '💡 临时设置 core.quotepath=false 以正确读取路径。' -ForegroundColor Yellow }
        }
    }
    catch {}

    # ---------------------------
    # 读取现有 .gitattributes 中的 LFS 规则
    # ---------------------------
    $gitAttributesPath = Join-Path -Path $absoluteProjectPath -ChildPath ".gitattributes"
    $existingLfsPatterns = @{ }
    if (Test-Path -LiteralPath $gitAttributesPath) {
        try {
            $attributesContent = Get-Content -LiteralPath $gitAttributesPath -ErrorAction Stop
            $lfsLines = $attributesContent | Where-Object { $_ -match 'filter=lfs' }
            foreach ($line in $lfsLines) {
                $pattern = ($line -split '\s+')[0].Trim()
                if ($pattern -and -not $existingLfsPatterns.ContainsKey($pattern)) {
                    $existingLfsPatterns[$pattern] = $true
                }
            }
            Write-Host ("📄 检测到 .gitattributes 并读取 {0} 条 LFS 规则。" -f $existingLfsPatterns.Keys.Count)
        }
        catch {
            Write-Host '⚠️ 无法读取 .gitattributes 文件（忽略）。' -ForegroundColor Yellow
        }
    }

    # ---------------------------
    # 获取文件列表与 LFS 列表
    # ---------------------------
    try {
        $filePaths = Get-GitTrackedFiles -repo $absoluteProjectPath
    }
    catch {
        Write-Host ("❌ 无法获取 Git 跟踪文件：{0}" -f $_.Exception.Message) -ForegroundColor Red
        # 即使这里出错，finally 块也会执行恢复操作，因此直接 return
        return
    }

    Write-Host ("📦 检测到 {0} 个被 Git 追踪的文件。" -f $filePaths.Count)

    $lfsList = Get-GitLfsFiles -repo $absoluteProjectPath
    $lfsSet = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($p in $lfsList) { $lfsSet.Add($p) | Out-Null }

    # ---------------------------
    # 收集文件信息
    # ---------------------------
    Write-Host ("🔍 正在分析文件详情与抽样信息...（忽略扩展: {0})" -f ($IgnoreExtensions -join ', '))
    $totalFiles = $filePaths.Count
    $i = 0
    $fileObjects = @()
    $missingFilesCount = 0

    foreach ($rel in $filePaths) {
        $i++
        Write-Progress -Activity "分析文件" -Status ("{0} / {1}" -f $i, $totalFiles) -PercentComplete ($i / $totalFiles * 100)

        if ([string]::IsNullOrWhiteSpace($rel)) { continue }
        $full = Join-Path -Path $absoluteProjectPath -ChildPath $rel
        try {
            $item = Get-Item -LiteralPath $full -ErrorAction Stop
            if ($item -and -not $item.PSIsContainer) {
                $ext = [IO.Path]::GetExtension($item.Name).ToLower()
                if ($IgnoreExtensions -contains $ext) { continue }
                $isLfs = $lfsSet.Contains($rel)
                $fileObjects += [PSCustomObject]@{
                    Name         = $item.Name
                    RelativePath = $rel
                    FullName     = $item.FullName
                    Extension    = if ($ext) { $ext } else { "(无扩展名)" }
                    Length       = $item.Length
                    IsLfsTracked = $isLfs
                }
            }
        }
        catch {
            $missingFilesCount++
            continue
        }
    }
    Write-Progress -Activity "分析文件" -Completed

    Write-Host ("`n✅ 文件分析完成：有效文件 {0}，缺失或不可访问的追踪文件 {1}。" -f $fileObjects.Count, $missingFilesCount)

    # ---------------------------
    # 仓库大小估算
    # ---------------------------
    $totalSize = ($fileObjects | Measure-Object -Property Length -Sum).Sum
    $gitObjectsSize = 0
    try {
        $gitObjectsPath = Join-Path -Path $gitDir -ChildPath "objects"
        if (Test-Path -LiteralPath $gitObjectsPath) {
            $gitObjectsSize = (Get-ChildItem -Path $gitObjectsPath -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        }
    }
    catch {}
    $approxTotal = $totalSize + $gitObjectsSize

    # ---------------------------
    # 生成统计：按扩展名聚合
    # ---------------------------
    $fileStats = $fileObjects | Group-Object -Property Extension | ForEach-Object {
        $sum = ($_.Group | Measure-Object -Property Length -Sum).Sum
        $max = ($_.Group | Measure-Object -Property Length -Maximum).Maximum
        $avg = ($_.Group | Measure-Object -Property Length -Average).Average
        $extName = if ([string]::IsNullOrEmpty($_.Name)) { "(无扩展名)" } else { $_.Name }
        
        # 正确地获取类型和描述，处理键不存在的情况
        $type = $extensionTypeMap[$extName]
        if ([string]::IsNullOrEmpty($type)) { $type = '未知' }

        $description = $extensionNameMap[$extName]
        if ([string]::IsNullOrEmpty($description)) { $description = '未知用途' }

        [PSCustomObject]@{
            Extension      = $extName
            Type           = $type
            Description    = $description
            Count          = $_.Count
            TotalSizeBytes = $sum
            TotalSize      = Format-Bytes $sum
            MaxSize        = Format-Bytes $max
            AvgSize        = Format-Bytes $avg
            AvgSizeBytes   = [int64]$avg
            Group          = $_.Group
        }
    } | Sort-Object -Property TotalSizeBytes -Descending

    # ---------------------------
    # 输出报告头（控制台）
    # ---------------------------
    Write-Host ""
    Write-Host '===========================================================' -ForegroundColor Green
    Write-Host '      Git 仓库文件分析报告' -ForegroundColor Green
    Write-Host '===========================================================' -ForegroundColor Green
    Write-Host ("项目路径: {0}" -f $absoluteProjectPath)
    Write-Host ("被 Git 追踪的文件数: {0}" -f $filePaths.Count)
    Write-Host ("有效分析文件数: {0}" -f $fileObjects.Count)
    if ($missingFilesCount -gt 0) { Write-Host ("缺失/不可访问的追踪文件数: {0}（请检查并执行 git lfs pull）" -f $missingFilesCount) -ForegroundColor Yellow }
    Write-Host ("仓库总文件大小 (估算): {0} ，.git objects 大小: {1} ，合计估算: {2}" -f (Format-Bytes $totalSize), (Format-Bytes $gitObjectsSize), (Format-Bytes $approxTotal))
    Write-Host ("报告时间: {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))

    # ---------------------------
    # 控制台：按扩展名统计
    # ---------------------------
    Write-Host "`n--- 按扩展名统计 (按总占用降序，显示前 20) ---" -ForegroundColor Cyan
    $extTableProps = if ($ShowExtensionDetails) {
        @('Extension', 'Type', 'Description', 'Count', 'TotalSize', 'AvgSize')
    }
    else {
        @('Extension', 'Count', 'TotalSize', 'AvgSize')
    }
    $fileStats | Select-Object -First 20 | Format-Table -Property $extTableProps -AutoSize

    # ---------------------------
    # 目录占用统计
    # ---------------------------
    if (-not $NoDirectoryAnalysis) {
        $dirMap = @{}
        foreach ($fo in $fileObjects) {
            $parts = $fo.RelativePath -split "[\\/]"
            $take = [Math]::Min($DirectoryDepth, $parts.Count)
            $dirKey = if ($take -le 0) { "(根目录)" } else { ($parts[0..($take - 1)] -join "/") }

            if (-not $dirMap.ContainsKey($dirKey)) { $dirMap[$dirKey] = [PSCustomObject]@{ Total = 0; Count = 0 } }
            $dirMap[$dirKey].Total += $fo.Length
            $dirMap[$dirKey].Count += 1
        }
        $dirStats = $dirMap.GetEnumerator() | ForEach-Object {
            [PSCustomObject]@{ Directory = $_.Key; Count = $_.Value.Count; TotalSizeBytes = $_.Value.Total; TotalSize = Format-Bytes $_.Value.Total }
        } | Sort-Object -Property TotalSizeBytes -Descending | Select-Object -First 20
        Write-Host "`n--- 按目录占用 (Top 20) ---" -ForegroundColor Cyan
        $dirStats | Format-Table -AutoSize
    }

    # ---------------------------
    # Top N 大文件
    # ---------------------------
    Write-Host ("`n--- Top {0} 大文件 (显示 LFS 状态) ---" -f $TopNFiles) -ForegroundColor Cyan
    $topFiles = $fileObjects | Sort-Object -Property Length -Descending | Select-Object -First $TopNFiles
    $topDisplay = $topFiles | ForEach-Object {
        [PSCustomObject]@{
            大小    = Format-Bytes $_.Length
            LFS状态 = if ($_.IsLfsTracked) { "✅" } else { "❌" }
            路径    = $_.RelativePath
        }
    }
    $topDisplay | Format-Table -AutoSize

    # ---------------------------
    # LFS 覆盖率统计
    # ---------------------------
    $totalLfsTrackedCount = ($fileObjects | Where-Object { $_.IsLfsTracked }).Count
    $lfsCoveragePercent = if ($fileObjects.Count -gt 0) { [math]::Round(($totalLfsTrackedCount / $fileObjects.Count) * 100, 2) } else { 0 }
    Write-Host ("`n📊 LFS 覆盖率: {0}% ({1} / {2})" -f $lfsCoveragePercent, $totalLfsTrackedCount, $fileObjects.Count) -ForegroundColor Yellow

    # ---------------------------
    # LFS 建议生成
    # ---------------------------
    $recommendedRules = @()
    $warningRules = @()
    $existingRules = @()
    $noExtLargeBinaryFiles = @()

    if (-not $NoLfsSuggestions) {
        Write-Host "`n===========================================================" -ForegroundColor Yellow
        Write-Host "      LFS 规则智能分析与建议 (.gitattributes)" -ForegroundColor Yellow
        Write-Host "===========================================================" -ForegroundColor Yellow

        if ($missingFilesCount -gt 0) {
            Write-Host ("⚠️ 发现 {0} 个追踪文件在磁盘上缺失，建议先运行 `git lfs pull` 并重试以获得最准确结果。" -f $missingFilesCount) -ForegroundColor Red
        }

        foreach ($stat in $fileStats) {
            $ext = $stat.Extension
            if ($ext -eq "(无扩展名)") {
                # 对于无扩展名的文件，不生成 .gitattributes 规则，但收集其为单独的警告
                $noExtCandidates = $stat.Group | Where-Object { 
                    -not $_.IsLfsTracked -and 
                    $_.Length -gt $SizeThreshold -and 
                    (Test-IsBinary -FilePath $_.FullName) 
                }
                if ($noExtCandidates.Count -gt 0) {
                    $noExtLargeBinaryFiles += [PSCustomObject]@{
                        Extension      = $ext
                        Type           = $stat.Type
                        Description    = $stat.Description
                        Count          = $noExtCandidates.Count
                        TotalSizeBytes = ($noExtCandidates | Measure-Object -Property Length -Sum).Sum
                        Samples        = $noExtCandidates | Sort-Object Length -Descending | Select-Object -First $TopMSamples
                    }
                }
                continue # 跳过对无扩展名文件生成常规 LFS 规则
            }
            
            if ($IgnoreExtensions -contains $ext) { continue }
            if ($stat.AvgSizeBytes -lt ([int64]($SizeThreshold / 10))) { continue }

            $pattern = "*$ext"
            if ($existingLfsPatterns.ContainsKey($pattern)) {
                $existingRules += $pattern
                continue
            }

            $sampleCount = [Math]::Min($SampleCount, $stat.Count)
            $samples = if ($stat.Group.Count -le $sampleCount) { $stat.Group } else { $stat.Group | Get-Random -Count $sampleCount }
            $votes = ($samples | ForEach-Object { if (Test-IsBinary -FilePath $_.FullName) { 1 } else { 0 } } | Measure-Object -Sum).Sum
            $isBinary = ($votes -ge [Math]::Ceiling($sampleCount / 2))

            if ($isBinary -and -not ($KnownTextExtensions -contains $ext)) {
                $recommendedRules += [PSCustomObject]@{ 
                    Extension      = $ext; 
                    Pattern        = $pattern; 
                    Type           = $stat.Type;
                    TotalSizeBytes = $stat.TotalSizeBytes; 
                    Count          = $stat.Count; 
                    AvgSize        = $stat.AvgSize 
                }
            }
            elseif ($KnownTextExtensions -contains $ext -and $stat.AvgSizeBytes -gt $SizeThreshold) {
                $warningRules += [PSCustomObject]@{ 
                    Extension      = $ext; 
                    Pattern        = $pattern; 
                    Type           = $stat.Type;
                    AvgSize        = $stat.AvgSize; 
                    Count          = $stat.Count; 
                    TotalSizeBytes = $stat.TotalSizeBytes 
                }
            }
        }

        $recommendedRulesGrouped = $recommendedRules | Group-Object -Property Type | ForEach-Object {
            [PSCustomObject]@{
                Type  = $_.Name;
                Rules = $_.Group | Sort-Object -Property TotalSizeBytes -Descending
            }
        } | Sort-Object -Property @{ Expression = { ($_.Rules | Measure-Object -Property TotalSizeBytes -Sum).Sum }; Descending = $true }

        $warningRulesGrouped = $warningRules | Group-Object -Property Type | ForEach-Object {
            [PSCustomObject]@{
                Type  = $_.Name;
                Rules = $_.Group | Sort-Object -Property TotalSizeBytes -Descending
            }
        } | Sort-Object -Property @{ Expression = { ($_.Rules | Measure-Object -Property TotalSizeBytes -Sum).Sum }; Descending = $true }


        if ($existingRules.Count -gt 0) {
            Write-Host "`n✅ 已存在 LFS 规则（无需操作）：" -ForegroundColor DarkGreen
            foreach ($r in $existingRules) { Write-Host ("  {0}" -f $r) -ForegroundColor DarkGreen }
        }

        if ($warningRules.Count -gt 0) {
            Write-Host "`n⚠️ 警告性规则（文本文件但体积较大，加入 LFS 会影响合并能力，请谨慎评估）：" -ForegroundColor Magenta
            foreach ($group in $warningRulesGrouped) {
                Write-Host ("  --- {0} ({1})" -f $group.Type, (Format-Bytes (($group.Rules | Measure-Object -Property TotalSizeBytes -Sum).Sum))) -ForegroundColor DarkYellow
                foreach ($w in $group.Rules) {
                    Write-Host ("    {0}   # 平均大小: {1}, 共 {2} 个文件" -f $w.Pattern, $w.AvgSize, $w.Count) -ForegroundColor Magenta
                }
            }
        }

        if ($recommendedRules.Count -gt 0) {
            Write-Host "`n🚀 推荐新增 LFS 规则（按类型分组，同类型按总占用降序）：" -ForegroundColor White
            foreach ($group in $recommendedRulesGrouped) {
                Write-Host ("  --- {0} ({1})" -f $group.Type, (Format-Bytes (($group.Rules | Measure-Object -Property TotalSizeBytes -Sum).Sum))) -ForegroundColor DarkCyan
                foreach ($r in $group.Rules) {
                    Write-Host ("    {0} filter=lfs diff=lfs merge=lfs -text   # 总占用: {1}, 文件数: {2}, 平均: {3}" -f $r.Pattern, (Format-Bytes $r.TotalSizeBytes), $r.Count, $r.AvgSize)
                }
            }
        }

        if ($noExtLargeBinaryFiles.Count -gt 0) {
            Write-Host ("`n❗ 发现未被 LFS 跟踪的无扩展名大二进制文件 (共 {0} 组，请考虑手动跟踪):" -f $noExtLargeBinaryFiles.Count) -ForegroundColor Red
            foreach ($noExtGroup in $noExtLargeBinaryFiles | Sort-Object TotalSizeBytes -Descending) {
                Write-Host ("  --- (无扩展名) 总占用: {0}, 文件数: {1} (Top {2} 样本):" -f (Format-Bytes $noExtGroup.TotalSizeBytes), $noExtGroup.Count, $TopMSamples) -ForegroundColor DarkRed
                foreach ($sample in $noExtGroup.Samples) {
                    Write-Host ("    {0,-12} {1}" -f (Format-Bytes $sample.Length), $sample.RelativePath) -ForegroundColor Red
                }
            }
        }
        
        if ($recommendedRules.Count -eq 0 -and $warningRules.Count -eq 0 -and $noExtLargeBinaryFiles.Count -eq 0) {
            Write-Host "`n👍 未发现需要新增 LFS 规则或特殊处理的文件。" -ForegroundColor Green
        }
    }

    # ---------------------------
    # 导出报告
    # ---------------------------
    if (-not [string]::IsNullOrWhiteSpace($ExportReport)) {
        try {
            $reportPath = [IO.Path]::GetFullPath($ExportReport)
            $reportDir = [IO.Path]::GetDirectoryName($reportPath)
            if ($reportDir -and -not (Test-Path -LiteralPath $reportDir)) {
                New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
            }
            Write-Host ("`n💾 正在导出报告到: {0}" -f $reportPath) -ForegroundColor Cyan

            $md = @()
            $md += '# Git 仓库 LFS 分析报告'
            $md += ''
            $md += ('*项目路径*: `{0}`' -f $absoluteProjectPath)
            $md += ('*生成时间*: {0}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
            $md += ''
            $md += '## 概览'
            $md += ('- 被 Git 追踪的文件数: {0}' -f $filePaths.Count)
            $md += ('- 有效分析文件数: {0}' -f $fileObjects.Count)
            $md += ('- 缺失/不可访问追踪文件: {0}' -f $missingFilesCount)
            $md += ('- 仓库总文件大小: {0}' -f (Format-Bytes $totalSize))
            $md += ('- .git objects 大小 (估算): {0}' -f (Format-Bytes $gitObjectsSize))
            $md += ('- 合计估算: {0}' -f (Format-Bytes $approxTotal))
            $md += ('- LFS 覆盖率: {0}% ({1} / {2})' -f $lfsCoveragePercent, $totalLfsTrackedCount, $fileObjects.Count)
            $md += ""

            $md += "## Top $TopNFiles 大文件（含 LFS 状态）"
            $md += ""
            $md += "| 大小 | LFS | 路径 |"
            $md += "| ---: | :--: | --- |"
            foreach ($tf in $topFiles) {
                $lfsStatusEmoji = ""
                if ($tf.IsLfsTracked) {
                    $lfsStatusEmoji = "✅"
                }
                else {
                    $lfsStatusEmoji = "❌"
                }
                $md += ("| {0} | {1} | `{2}` |" -f (Format-Bytes $tf.Length), $lfsStatusEmoji, $tf.RelativePath.Replace('\', '/'))
            }

            $md += ""
            $md += "## 按扩展名统计（Top 20）"
            $md += ""
            if ($ShowExtensionDetails) {
                $md += "| 扩展名 | 类型 | 描述 | 数量 | 总占用 | 平均大小 |"
                $md += "| --- | --- | --- | ---: | ---: | ---: |"
                $fileStats | Select-Object -First 20 | ForEach-Object {
                    $md += ("| {0} | {1} | {2} | {3} | {4} | {5} |" -f $_.Extension, $_.Type, $_.Description, $_.Count, $_.TotalSize, $_.AvgSize)
                }
            }
            else {
                $md += "| 扩展名 | 数量 | 总占用 | 平均大小 |"
                $md += "| --- | ---: | ---: | ---: |"
                $fileStats | Select-Object -First 20 | ForEach-Object {
                    $md += ("| {0} | {1} | {2} | {3} |" -f $_.Extension, $_.Count, $_.TotalSize, $_.AvgSize)
                }
            }

            if (-not $NoDirectoryAnalysis) {
                $md += ""
                $md += "## 按目录占用（Top 20）"
                $md += ""
                $md += "| 目录 | 文件数 | 总占用 |"
                $md += "| --- | ---: | ---: |"
                foreach ($d in $dirStats) { $md += ("| `{0}` | {1} | {2} |" -f $d.Directory, $d.Count, $d.TotalSize) }
            }
        
            $md += ""
            $md += "## LFS 建议"
            if ($existingRules.Count -gt 0) {
                $md += "### ✅ 已存在规则"
                foreach ($r in $existingRules) { $md += "- `{0}` " -f $r }
            }
            if ($warningRules.Count -gt 0) {
                $md += "### ⚠️ 警告规则（文本文件但体积较大，加入 LFS 会影响合并能力，请谨慎评估）"
                foreach ($group in $warningRulesGrouped) {
                    $md += ""
                    $md += ("**{0}** (总占用: {1}):" -f $group.Type, (Format-Bytes (($group.Rules | Measure-Object -Property TotalSizeBytes -Sum).Sum)))
                    foreach ($w in $group.Rules) { $md += ("- `{0}`  # 平均大小: {1}, 文件数: {2}" -f $w.Pattern, $w.AvgSize, $w.Count) }
                }
            }
            if ($recommendedRules.Count -gt 0) {
                $md += "### 🚀 推荐新增规则（按类型分组，同类型按总占用降序）"
                foreach ($group in $recommendedRulesGrouped) {
                    $md += ""
                    $md += ("**{0}** (总占用: {1}):" -f $group.Type, (Format-Bytes (($group.Rules | Measure-Object -Property TotalSizeBytes -Sum).Sum)))
                    foreach ($r in $group.Rules) {
                        $md += ("- `{0} filter=lfs diff=lfs merge=lfs -text`  # 总占用: {1}, 文件数: {2}, 平均: {3}" -f $r.Pattern, (Format-Bytes $r.TotalSizeBytes), $r.Count, $r.AvgSize)
                    }
                }
            }
            if ($noExtLargeBinaryFiles.Count -gt 0) {
                $md += "### ❗ 无扩展名大二进制文件 (请考虑手动跟踪)"
                foreach ($noExtGroup in $noExtLargeBinaryFiles | Sort-Object TotalSizeBytes -Descending) {
                    $md += ""
                    $md += ("- 无扩展名文件总占用: **{0}**, 文件数: **{1}** (Top {2} 样本):" -f (Format-Bytes $noExtGroup.TotalSizeBytes), $noExtGroup.Count, $TopMSamples)
                    foreach ($sample in $noExtGroup.Samples) {
                        $md += ("  - `{0}` ({1})" -f $sample.RelativePath.Replace('\', '/'), (Format-Bytes $sample.Length))
                    }
                }
            }
        
            if (-not $NoLfsSuggestions) {
                if ($recommendedRules.Count -eq 0 -and $warningRules.Count -eq 0 -and $noExtLargeBinaryFiles.Count -eq 0) {
                    $md += "👍 未发现需要新增的 LFS 规则或特殊处理的文件。"
                }
            }
            else {
                $md += "（LFS 建议已被禁用）"
            }
        
            $md += ""
            $md += "## 未被 LFS 跟踪的大文件样本（Top $TopMSamples）"
            $md += ""
            $md += "| 大小 | 路径 |"
            $md += "| ---: | --- |"

            $noExtSamplePaths = [System.Collections.Generic.HashSet[string]]::new()
            if ($noExtLargeBinaryFiles.Count -gt 0) {
                $noExtLargeBinaryFiles | ForEach-Object { $_.Samples } | ForEach-Object { $noExtSamplePaths.Add($_.RelativePath) | Out-Null }
            }

            $generalSampleFiles = $fileObjects | Where-Object { 
                -not $_.IsLfsTracked -and 
                $_.Length -gt $SizeThreshold -and
                -not ($noExtSamplePaths.Contains($_.RelativePath))
            } | Sort-Object Length -Descending | Select-Object -First $TopMSamples

            if ($generalSampleFiles.Count -eq 0) {
                $md += "| *未发现其他符合条件的未LFS大文件样本* | |"
            }
            else {
                $generalSampleFiles | ForEach-Object { $md += ("| {0} | `{1}` |" -f (Format-Bytes $_.Length), $_.RelativePath.Replace('\', '/')) }
            }

            $mdContent = $md -join "`r`n"
            Set-Content -LiteralPath $reportPath -Value $mdContent -Encoding UTF8
            Write-Host ("✅ 报告已导出到: {0}" -f $reportPath) -ForegroundColor Green

        }
        catch {
            Write-Host "`n"
            Write-Host "!导出报告时发生严重错误!" -ForegroundColor Red
            Write-Host ("`n错误类型: {0}" -f $_.Exception.GetType().FullName) -ForegroundColor Yellow
            Write-Host ("错误信息: {0}" -f $_.Exception.Message) -ForegroundColor Yellow
            Write-Host ("错误位置: 行 {0}" -f $_.InvocationInfo.ScriptLineNumber) -ForegroundColor Yellow
            Write-Host ("详细堆栈: `n{0}" -f $_.Exception.StackTrace) -ForegroundColor Yellow
            Write-Host "`n"
        }
    }

}
finally {
    # ---------------------------
    # 恢复 core.quotepath (此块总会执行)
    # ---------------------------
    if ($didChangeQuotePath) {
        Write-Host "`n🔧 分析完成，正在恢复 core.quotepath..." -ForegroundColor Yellow
        try {
            if ($quotePathIsSet) {
                git -C $absoluteProjectPath config core.quotepath $originalQuotePath 2>$null
            }
            else {
                git -C $absoluteProjectPath config --unset core.quotepath 2>$null
            }
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Git 配置已恢复（core.quotepath）。" -ForegroundColor Green
            }
            else {
                Write-Host "⚠️ 恢复 core.quotepath 失败，请手动检查 git config core.quotepath" -ForegroundColor Red
            }
        }
        catch {
            Write-Host "⚠️ 恢复 core.quotepath 时出错，请手动检查该配置。" -ForegroundColor Yellow
        }
    }
}

Write-Host "`n🎯 分析与诊断完成。"
exit 0