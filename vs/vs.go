package vs

import (
	"fmt"
	"github.com/jurgen-kluft/xcode/denv"
)

// IsVisualStudio returns true if the incoming string @ide is equal to any of
// the Visual Studio formats that xcode supports.
func IsVisualStudio(dev string, os string, arch string) bool {
	vs := GetVisualStudio(dev)
	return vs != -1
}

// GetVisualStudio returns a value for type IDE deduced from the incoming string @ide
func GetVisualStudio(dev string) denv.DEV {
	if dev == "VS2017" {
		return denv.VS2017
	} else if dev == "VS2015" {
		return denv.VS2015
	} else if dev == "VS2013" {
		return denv.VS2013
	} else if dev == "VS2012" {
		return denv.VS2012
	}
	return -1
}

// GenerateVisualStudioSolutionAndProjects will generate the Solution and Project files for the incoming project
func GenerateVisualStudioSolutionAndProjects(dev denv.DEV, path string, targets []string, pkg *denv.Package) error {

	prj := pkg.GetMainApp()
	if prj == nil {
		prj = pkg.GetUnittest()
	}
	if prj == nil {
		return fmt.Errorf("This package has no main app or main test")
	}

	GenerateVisualStudio2015Solution(prj)

	return fmt.Errorf("Unsupported Visual Studio version")
}
