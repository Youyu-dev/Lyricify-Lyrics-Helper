#!/bin/bash

# Lyricify.Lyrics.Api 自包含发布脚本
# 用法：./publish.sh [platform]
# 平台选项：osx-arm64, osx-x64, win-x64, linux-x64, all

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR"
OUTPUT_BASE="$PROJECT_DIR/publish"

# 颜色输出
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_info() {
    echo -e "${BLUE}[信息]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[成功]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[警告]${NC} $1"
}

publish_platform() {
    local platform=$1
    local output_dir="$OUTPUT_BASE/$platform"
    
    print_info "正在为 $platform 平台发布..."
    
    # 删除旧的发布文件
    if [ -d "$output_dir" ]; then
        rm -rf "$output_dir"
    fi
    
    # 执行发布
    dotnet publish -c Release -r "$platform" --self-contained true -o "$output_dir"
    
    if [ $? -eq 0 ]; then
        # 获取发布文件大小
        if [ -d "$output_dir" ]; then
            local size=$(du -sh "$output_dir" | cut -f1)
            print_success "$platform 发布完成！输出目录: $output_dir (大小: $size)"
        fi
    else
        print_warning "$platform 发布失败"
        return 1
    fi
}

# 主逻辑
PLATFORM="${1:-all}"

print_info "开始自包含发布..."
print_info "项目目录: $PROJECT_DIR"
print_info "输出目录: $OUTPUT_BASE"

case $PLATFORM in
    osx-arm64|osx-x64|win-x64|linux-x64)
        publish_platform "$PLATFORM"
        ;;
    all)
        print_info "发布所有平台..."
        publish_platform "osx-arm64"
        publish_platform "osx-x64"
        publish_platform "win-x64"
        publish_platform "linux-x64"
        ;;
    *)
        echo "错误：不支持的平台 '$PLATFORM'"
        echo "支持的平台: osx-arm64, osx-x64, win-x64, linux-x64, all"
        exit 1
        ;;
esac

print_success "所有发布任务完成！"
print_info "使用方法："
print_info "  macOS: ./publish/$PLATFORM/Lyricify.Lyrics.Api"
print_info "  Windows: publish\\$PLATFORM\\Lyricify.Lyrics.Api.exe"
print_info "  Linux: ./publish/$PLATFORM/Lyricify.Lyrics.Api"
