Name: ProcessTracker
Version: %{version}
Release: %{release}%{?dist}
Summary: A tool to track and report how long process were running.
License: MIT
BuildArch: %{buildarch}

# BuildRequires: systemd-rpm-macros
Requires: dotnet-runtime-8.0 >= 8.0.4

%global name_lower processtracker
%global __strip /bin/true
%define _build_id_links none
# workaround for missing systemd rpm macros package on github agents
%global _unitdir %{_prefix}/lib/systemd/system

%description
A tool to track and report how long process were running.

%install
rm -rf $RPM_BUILD_ROOT

mkdir -p $RPM_BUILD_ROOT/opt/%{name_lower}
mkdir -p $RPM_BUILD_ROOT/%{_datadir}/doc/%{name_lower}
mkdir -p $RPM_BUILD_ROOT/%{_sysconfdir}/%{name_lower}
mkdir -p $RPM_BUILD_ROOT/%{_unitdir}

install -m 755 %{_sourcedir}/%{name_lower} $RPM_BUILD_ROOT/opt/%{name_lower}
install -m 644 %{_sourcedir}/*.so $RPM_BUILD_ROOT/opt/%{name_lower}/
install -m 644 %{_sourcedir}/%{name_lower}.service $RPM_BUILD_ROOT/%{_unitdir}
install -m 644 %{_sourcedir}/appsettings.json $RPM_BUILD_ROOT/%{_sysconfdir}/%{name_lower}
install -m 644 %{_sourcedir}/README.md $RPM_BUILD_ROOT/%{_datadir}/doc/%{name_lower}
install -m 644 %{_sourcedir}/LICENSE $RPM_BUILD_ROOT/%{_datadir}/doc/%{name_lower}

%files
%attr(0755, root, root) /opt/%{name_lower}/%{name_lower}
%attr(0644, root, root) /opt/%{name_lower}/*.so
%attr(0644, root, root) %{_unitdir}/%{name_lower}.service
%attr(0644, root, root) %{_datadir}/doc/%{name_lower}/README.md
%attr(0644, root, root) %{_datadir}/doc/%{name_lower}/LICENSE
%config(noreplace) %{_sysconfdir}/%{name_lower}/appsettings.json  
%dir /opt/%{name_lower}
%dir %{_datadir}/doc/%{name_lower}

%post
systemctl daemon-reload
if [ $1 -eq 1 ]; then  # Upgrade
    systemctl start %{name_lower}.service
else  # New installation
    systemctl enable %{name_lower}.service
    systemctl start %{name_lower}.service
fi

%preun
if [ $1 -eq 0 ]; then  # Uninstall
    systemctl stop %{name_lower}.service
    systemctl disable %{name_lower}.service
    echo "The configuration file /etc/processtracker/appsettings.json has not been removed."
    echo "If you want to delete it, remove it manually with 'rm /etc/processtracker/appsettings.json'"
elif [ $1 -eq 1 ]; then  # Upgrade
    systemctl stop %{name_lower}.service
fi

%postun
if [ $1 -eq 0 ] ; then
    systemctl daemon-reload
    echo "%{name_lower} service removed"
fi
