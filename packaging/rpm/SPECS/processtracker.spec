Name: processtracker
Version: %{version}
Release: %{release}%{?dist}
Summary: A tool to track and report how long process were running.
License: MIT
BuildArch: %{buildarch}

# BuildRequires: systemd-rpm-macros
Requires: dotnet-runtime-8.0 >= 8.0.4

%global __strip /bin/true
%define _build_id_links none
# workaround for missing systemd rpm macros package on github agents
%global _unitdir %{_prefix}/lib/systemd/system

%description
A tool to track and report how long process were running.

%install
rm -rf $RPM_BUILD_ROOT

mkdir -p $RPM_BUILD_ROOT/opt/%{name}
mkdir -p $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}
mkdir -p $RPM_BUILD_ROOT/%{_sysconfdir}/%{name}
mkdir -p $RPM_BUILD_ROOT/%{_unitdir}

install -m 755 %{_sourcedir}/%{name} $RPM_BUILD_ROOT/opt/%{name}
install -m 644 %{_sourcedir}/*.so $RPM_BUILD_ROOT/opt/%{name}/
install -m 644 %{_sourcedir}/%{name}.service $RPM_BUILD_ROOT/%{_unitdir}
install -m 644 %{_sourcedir}/appsettings.json $RPM_BUILD_ROOT/%{_sysconfdir}/%{name}
install -m 644 %{_sourcedir}/README.md $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}
install -m 644 %{_sourcedir}/LICENSE $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}

%files
%attr(0755, root, root) /opt/%{name}/%{name}
%attr(0644, root, root) /opt/%{name}/*.so
%attr(0644, root, root) %{_unitdir}/%{name}.service
%attr(0644, root, root) %{_datadir}/doc/%{name}/README.md
%attr(0644, root, root) %{_datadir}/doc/%{name}/LICENSE
%config(noreplace) %{_sysconfdir}/%{name}/appsettings.json  
%dir /opt/%{name}
%dir %{_datadir}/doc/%{name}

%post
systemctl daemon-reload
if [ $1 -eq 1 ]; then  # Upgrade
    systemctl start %{name}.service
else  # New installation
    systemctl enable %{name}.service
    systemctl start %{name}.service
fi

%preun
if [ $1 -eq 0 ]; then  # Uninstall
    systemctl stop %{name}.service
    systemctl disable %{name}.service
    echo "The configuration file /etc/processtracker/appsettings.json has not been removed."
    echo "If you want to delete it, remove it manually with 'rm /etc/processtracker/appsettings.json'"
elif [ $1 -eq 1 ]; then  # Upgrade
    systemctl stop %{name}.service
fi

%postun
if [ $1 -eq 0 ] ; then
    systemctl daemon-reload
    echo "%{name} service removed"
fi
