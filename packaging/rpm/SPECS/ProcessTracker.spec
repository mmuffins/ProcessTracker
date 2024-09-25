Name: ProcessTracker
Version: %{version}
Release: %{release}%{?dist}
Summary: A tool to track and report how long process were running.
License: MIT
BuildArch: %{buildarch}

%global name_lower processtracker
%global __strip /bin/true

%description
A tool to track and report how long process were running.

%install
rm -rf $RPM_BUILD_ROOT

mkdir -p $RPM_BUILD_ROOT/%{_bindir}
mkdir -p $RPM_BUILD_ROOT/%{_datadir}/doc/%{name_lower}
mkdir -p $RPM_BUILD_ROOT/%{_datadir}/%{name_lower}
mkdir -p $RPM_BUILD_ROOT/%{_sysconfdir}/%{name_lower}
mkdir -p $RPM_BUILD_ROOT/%{_unitdir}

install -m 755 %{_sourcedir}/%{name_lower} $RPM_BUILD_ROOT/%{_bindir}
install -m 644 %{_sourcedir}/%{name_lower}.service $RPM_BUILD_ROOT/%{_unitdir}
install -m 644 %{_sourcedir}/appsettings.json $RPM_BUILD_ROOT/%{_sysconfdir}/%{name_lower}
install -m 644 %{_sourcedir}/README.md $RPM_BUILD_ROOT/%{_datadir}/doc/%{name_lower}
install -m 644 %{_sourcedir}/LICENSE $RPM_BUILD_ROOT/%{_datadir}/doc/%{name_lower}

%files
%attr(0755, root, root) %{_bindir}/%{name_lower}
%attr(0644, root, root) %{_unitdir}/%{name_lower}.service
%config(noreplace) %{_sysconfdir}/%{name_lower}/appsettings.json  
%attr(0644, root, root) %{_datadir}/doc/%{name_lower}/README.md
%attr(0644, root, root) %{_datadir}/doc/%{name_lower}/LICENSE
%dir %{_datadir}/%{name_lower}
%dir %{_datadir}/doc/%{name_lower}

%post
systemctl daemon-reload
systemctl enable %{name_lower}.service
systemctl start %{name_lower}.service

%preun
if [ $1 -eq 0 ]; then  # Only on uninstallation, not upgrade
    systemctl stop %{name_lower}.service
    systemctl disable %{name_lower}.service
    echo "The configuration file /etc/processtracker/appsettings.json has not been removed."
    echo "If you want to delete it, remove it manually with 'rm /etc/processtracker/appsettings.json'"
fi

%postun
if [ $1 -eq 0 ] ; then
    systemctl daemon-reload
    echo "%{name_lower} service removed"
fi