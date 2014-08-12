if exists("seek_loaded")
	finish
endif

let seek_loaded = 1

function! s:Seek(regex, ...)
	let old_efm = &efm
	set efm =\ %#%f(%l\\\,%c):\ %m

	let l:filter = exists("g:seek_filter") ? g:seek_filter : "*"
	if (a:0 == 1)
		let l:filter = a:1
	endif
	let l:command = $HOME . '/.vim/bundle/vim-win-seek/seek.exe -r ' . a:regex . ' -f ' . l:filter
	if exists("g:seek_dir")
		let l:command .= ' -d ' . g:seek_dir
	endif
	cgete system(l:command)
	copen

	let &efm = old_efm
endfunction

function! s:SeekDir(...)
	if (a:0 == 0)
		unlet g:seek_dir
	else
		let g:seek_dir = a:1
	endif
endfunction

:command! -nargs=+ Seek call s:Seek(<f-args>)
:command! -nargs=0 SeekWord call s:Seek('\b' . expand("<cword>") . '\b')
:command! -nargs=1 SeekFilter let g:seek_filter = <f-args>
:command! -nargs=? -complete=dir SeekDir call s:SeekDir(<f-args>)
