#!/usr/bin/env python3
"""
Local CI simulation script to test the pipeline steps locally.
Runs the same commands as the CI pipeline to catch issues before pushing.
"""
import subprocess
import sys
from pathlib import Path
import os

ROOT = Path(__file__).parent
SCRIPTS_DIR = ROOT / 'scripts'
GENERATORS_DIR = SCRIPTS_DIR / 'generators'
BUILD_DIR = ROOT / 'build'

def run_command(cmd, cwd=None, env_vars=None):
    """Run a command and return success/failure."""
    env = os.environ.copy()
    if env_vars:
        env.update(env_vars)
    # Fix Windows UTF-8 encoding issues
    env['PYTHONIOENCODING'] = 'utf-8'
    
    print(f"🔄 Running: {cmd}")
    try:
        result = subprocess.run(
            cmd,
            shell=True,
            cwd=cwd or ROOT,
            env=env,
            check=True,
            capture_output=True,
            text=True
        )
        print(f"✅ Success: {cmd}")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ Failed: {cmd}")
        print(f"   Exit code: {e.returncode}")
        print(f"   STDOUT: {e.stdout}")
        print(f"   STDERR: {e.stderr}")
        return False

def main():
    """Simulate the CI pipeline locally."""
    print("🚀 Starting local CI simulation...")
    
    # Set environment variable for empty specs allowance
    env_vars = {"ALLOW_EMPTY_SPECS": "1"}
    
    # Step 1: Spec Structure Validation
    print("\n📋 Step 1: Spec Structure Validation")
    if not run_command("py validate-spec-structure.py", cwd=SCRIPTS_DIR, env_vars=env_vars):
        print("❌ Spec structure validation failed")
        return 1
    
    # Step 2: Generate Spec Index
    print("\n🔍 Step 2: Generate Spec Index")
    if not run_command("py spec_parser.py", cwd=GENERATORS_DIR, env_vars=env_vars):
        print("❌ Spec index generation failed")
        return 1
    
    # Step 3: Build Traceability JSON
    print("\n🔗 Step 3: Build Traceability JSON")
    if not run_command("py build_trace_json.py", cwd=GENERATORS_DIR, env_vars=env_vars):
        print("❌ Traceability JSON generation failed")
        return 1
    
    # Step 4: Traceability Coverage Validation
    print("\n📊 Step 4: Traceability Coverage Validation")
    if not run_command("py validate-trace-coverage.py", cwd=SCRIPTS_DIR, env_vars=env_vars):
        print("❌ Traceability coverage validation failed")
        return 1
    
    # Check if required files exist
    print("\n📁 Step 5: Artifact Verification")
    required_files = [
        BUILD_DIR / 'spec-index.json',
        BUILD_DIR / 'traceability.json'
    ]
    
    for file_path in required_files:
        if file_path.exists():
            print(f"✅ Found: {file_path}")
        else:
            print(f"❌ Missing: {file_path}")
            return 1
    
    print("\n🎉 All CI pipeline steps completed successfully!")
    print("✅ Ready to push changes to trigger CI pipeline")
    return 0

if __name__ == "__main__":
    sys.exit(main())