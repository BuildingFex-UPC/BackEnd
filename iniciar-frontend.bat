@echo off
title BuildingFex - Frontend
cd /d "%~dp0..\Fronted"
echo Frontend en http://localhost:5173
echo API: Railway (backend-production-5e544.up.railway.app)
npm run dev
