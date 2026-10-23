#!/bin/bash
# 拉取 PCL2 原源码和 MeloongCore 到 vendor/ 目录。
# 运行前确保已安装 git 和 .NET 10 SDK。

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
VENDOR_DIR="$REPO_ROOT/vendor"

mkdir -p "$VENDOR_DIR"

if [ ! -d "$VENDOR_DIR/PCL2" ]; then
    echo "[setup] 克隆 PCL2 源码..."
    git clone --depth 1 https://github.com/Meloong-Git/PCL.git "$VENDOR_DIR/PCL2"
    cd "$VENDOR_DIR/PCL2"
    echo "[setup] 初始化 MeloongCore 子模块..."
    git submodule update --init --recursive
else
    echo "[setup] PCL2 已存在，跳过克隆"
fi

echo "[setup] 完成。现在可以运行："
echo "  cd PCL.CliTest && dotnet run"
