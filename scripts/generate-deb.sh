#!/bin/bash
VERSION=$1
PKG_DIR="gud_${VERSION}_amd64"

mkdir -p "$PKG_DIR/DEBIAN"
mkdir -p "$PKG_DIR/usr/local/bin/"

cp ./publish/linux-x64/gud "$PKG_DIR/usr/local/bin/gud"
chmod 755 "$PKG_DIR/usr/local/bin/gud"

cat > "$PKG_DIR/DEBIAN/control" <<EOF
package: gud
Version: $VERSION
Architecture: amd64
Maintainer: Duncan McPherson <d.mcpherson.home@gmail.com>
Section: utils
Priority: optional
Description: A git-inspired version control system
 gud is a content-addressable version control system
 built from scratch in C#
EOF

dpkg-deb --build --root-owner-group "$PKG_DIR"