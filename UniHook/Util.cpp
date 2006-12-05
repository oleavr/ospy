#include "stdafx.h"
#include "Util.h"
#include <iostream>
#include <sstream>
#include <iomanip>

// Thanks to Einar Otto Stangvik (http://einaros.livejournal.com/) for this neat little function...

std::string hexdump(void *x, unsigned long len, unsigned int w)
{
	std::ostringstream osDump;
	std::ostringstream osNums;
	std::ostringstream osChars;
	std::string szPrevNums;
	bool bRepeated = false;
	unsigned long i;

	for(i = 0; i <= len; i++) 
	{ 
		if(i < len) 
		{ 
			char c = (char)*((char*)x + i); 
			unsigned int n = (unsigned int)*((unsigned char*)x + i); 
			osNums << std::setbase(16) << std::setw(2) << std::setfill('0') << n << " "; 
			if(((i % w) != w - 1) && ((i % w) % 8 == 7)) 
			osNums << "- "; 
			osChars << (iscntrl(c) ? '.' : c); 
		}

		if(osNums.str().compare(szPrevNums) == 0) 
		{ 
			bRepeated = true; 
			osNums.str(""); 
			osChars.str(""); 
			if (i == len - 1) 
				osDump << "*" << std::endl; 
			continue; 
		} 

		if(((i % w) == w - 1) || ((i == len) && (osNums.str().size() > 0))) 
		{ 
			if(bRepeated) 
			{ 
				osDump << "*" << std::endl; 
				bRepeated = false; 
			} 
			osDump << std::setbase(16) << std::setw(8) << std::setfill('0') << (i - (i % w)) << "  " 
			   << std::setfill(' ') << std::setiosflags(std::ios_base::left) 
			   << std::setw(3 * w + ((w / 8) - 1) * 2) << osNums.str() 
			   << " |" << osChars.str() << std::resetiosflags(std::ios_base::left) << "|" << std::endl; 
			szPrevNums = osNums.str(); 
			osNums.str(""); 
			osChars.str(""); 
		} 
	}

	osDump << std::setbase(16) << std::setw(8) << std::setfill('0') << (i-1) << std::endl; 

	return osDump.str(); 
}
