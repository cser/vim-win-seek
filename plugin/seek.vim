if exists("seek_loaded")
	finish
endif

let seek_loaded = 1

function! s:ParseArgs(parts)
	let l:output = []
	let l:state = 0
	for part in a:parts
		if l:state == 0
			let l:output = l:output + [part]
			if strpart(part, 0, 1) == "'" && (strlen(part) == 1 || strpart(part, strlen(part) - 1, 1) != "'")
				let l:state = 1
			endif
		elseif l:state == 1
			let l:output[-1] = l:output[-1] . ' ' . part
			if strpart(part, strlen(part) - 1, 1) == "'"
				let l:state = 0
			endif
		endif
	endfor
	return l:output
endfunction

function! s:Seek(...)
	let old_efm = &efm
	set efm =\ %#%f(%l\\\,%c):\ %m
	let parsedArgs = s:ParseArgs(a:000)
	if len(parsedArgs) > 2
		throw "To many arguments (no more then 2 allowed): " . join(parsedArgs, ", ")
	endif

	let l:filter = exists("g:seek_filter") ? g:seek_filter : "*"
	if (len(parsedArgs) == 2)
		let l:filter = parsedArgs[1]
	endif
	let regex = parsedArgs[0]
	if strlen(regex) > 1 && strpart(regex, 0, 1) == "'" && strpart(regex, strlen(regex) - 1, 1) == "'"
		let regex = strpart(regex, 1, strlen(regex) - 2)
	endif
	echo "pattern: " . shellescape(regex)
	let l:command = $HOME . '/.vim/bundle/vim-win-seek/seek.exe -r ' . shellescape(regex) . ' -f ' . l:filter
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
:command! -nargs=0 SeekWord call s:Seek('/\b' . expand("<cword>") . '\b/')
:command! -nargs=1 SeekFilter let g:seek_filter = <f-args>
:command! -nargs=? -complete=dir SeekDir call s:SeekDir(<f-args>)
