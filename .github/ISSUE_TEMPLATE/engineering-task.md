---
name: Engineering task
about: Define a focused modernization, infrastructure, architecture, testing, or feature task
title: ""
labels: ""
assignees: ""
---

## Goal

<!-- Describe the outcome this issue should achieve. -->

## Relevant code

<!-- List the files, scenes, systems, packages, or documentation involved. -->

## Problem

<!-- Explain why the current state is insufficient or what behavior needs to change. -->

## Expected outcomes

<!-- List observable conditions that define completion. -->

- [ ]

## Validation

<!-- List the automated checks, Unity tests, builds, and manual scenarios required for this issue. -->

- [ ] `pwsh ./scripts/ci/Validate-Repository.ps1`
- [ ] `git fetch origin main && git diff --check origin/main...HEAD`
