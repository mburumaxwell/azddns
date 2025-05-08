# typed: false
# frozen_string_literal: true

class Azddns < Formula
  desc "Dynamic DNS (DDNS) tool for Azure DNS."
  homepage "https://github.com/mburumaxwell/azddns"
  license "MIT"
  version "#{VERSION}#"

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/mburumaxwell/azddns/releases/download/#{VERSION}#/azddns-#{VERSION}#-linux-arm64.tar.gz"
      sha256 "#{RELEASE_SHA256_LINUX_ARM64}#"
    end

    if Hardware::CPU.intel?
      url "https://github.com/mburumaxwell/azddns/releases/download/#{VERSION}#/azddns-#{VERSION}#-linux-x64.tar.gz"
      sha256 "#{RELEASE_SHA256_LINUX_X64}#"
    end
  end

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/mburumaxwell/azddns/releases/download/#{VERSION}#/azddns-#{VERSION}#-osx-arm64.tar.gz"
      sha256 "#{RELEASE_SHA256_MACOS_ARM64}#"
    end

    if Hardware::CPU.intel?
      url "https://github.com/mburumaxwell/azddns/releases/download/#{VERSION}#/azddns-#{VERSION}#-osx-x64.tar.gz"
      sha256 "#{RELEASE_SHA256_MACOS_X64}#"
    end
  end

  def install
    bin.install "azddns"
  end

  test do
    system "#{bin}/azddns", "--version"
  end
end
