BITS 32

mov ecx, (16384 / 4)
sub esp, 16384
push esi
push edi
cld
lea edi, [esp + 8]
lea esi, [edi + 16384]
rep movsd
pop edi
pop esi
