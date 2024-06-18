@echo off
@del /q *.o *.dbg *.map *.nes *.fds

CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1", rom_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1", rom_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_PAL_SUPPORT=1", rom_pal, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_PAL_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1", rom_pal_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_MMC5=1", rom_mmc5_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_MMC5=1", rom_mmc5_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=1", rom_n163_1ch_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=1", rom_n163_1ch_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=2", rom_n163_2ch_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=2", rom_n163_2ch_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=3", rom_n163_3ch_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=3", rom_n163_3ch_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=4", rom_n163_4ch_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=4", rom_n163_4ch_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=5", rom_n163_5ch_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=5", rom_n163_5ch_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=6", rom_n163_6ch_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=6", rom_n163_6ch_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=7", rom_n163_7ch_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=7", rom_n163_7ch_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=8", rom_n163_8ch_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_N163=1 -D FAMISTUDIO_EXP_N163_CHN_CNT=8", rom_n163_8ch_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_S5B=1", rom_s5b_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_S5B=1", rom_s5b_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_VRC6=1", rom_vrc6_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_VRC6=1", rom_vrc6_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_VRC7=1", rom_vrc7_ntsc, nes
CALL :CompileNsfPermutation rom.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_VRC7=1", rom_vrc7_ntsc_famitracker, nes
CALL :CompileNsfPermutation rom_epsm.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_EPSM=1", rom_epsm_ntsc, nes
CALL :CompileNsfPermutation rom_epsm.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_EPSM=1", rom_epsm_ntsc_famitracker, nes
CALL :CompileNsfPermutation fds.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_EXP_FDS=1", fds, fds
CALL :CompileNsfPermutation fds.cfg, "-D FAMISTUDIO_CFG_NTSC_SUPPORT=1 -D FAMISTUDIO_USE_FAMITRACKER_TEMPO=1 -D FAMISTUDIO_EXP_FDS=1", fds_famitracker, fds

EXIT /B %ERRORLEVEL%

:CompileNsfPermutation
@echo %~3
..\..\Tools\ca65 rom.s -g -o %~3.o %~2
..\..\Tools\ld65 -C %~1 -o %~3.%~4 %~3.o --mapfile %~3.map --dbgfile %~3.dbg
ExtractNoteTable %~3.dbg %~4
EXIT /B 0
