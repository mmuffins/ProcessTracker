Name: ProcessTracker
Version: %{version}
Release: %{release}%{?dist}
Summary: A tool to track and report how long process were running.
License: MIT
BuildArch: %{buildarch}

%global __strip /bin/true

%description
A tool to track and report how long process were running.

%install
rm -rf $RPM_BUILD_ROOT

mkdir -p $RPM_BUILD_ROOT/%{_bindir}
mkdir -p $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}
mkdir -p $RPM_BUILD_ROOT/%{_datadir}/%{name}

install -m 755 %{_sourcedir}/%{name} $RPM_BUILD_ROOT/%{_bindir}
install -m 644 %{_sourcedir}/%{name}.service $RPM_BUILD_ROOT/%{_datadir}/%{name}
install -m 644 %{_sourcedir}/appsettings.json $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}
install -m 644 %{_sourcedir}/README.md $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}
install -m 644 %{_sourcedir}/LICENSE $RPM_BUILD_ROOT/%{_datadir}/doc/%{name}

%files
%attr(0755, root, root) %{_bindir}/%{name}
%attr(0644, root, root) %{_datadir}/%{name}/%{name}.service
%attr(0644, root, root) %{_datadir}/doc/%{name}/appsettings.json
%attr(0644, root, root) %{_datadir}/doc/%{name}/README.md
%attr(0644, root, root) %{_datadir}/doc/%{name}/LICENSE
%dir %{_datadir}/%{name}
%dir %{_datadir}/doc/%{name}

%post
echo "To configure the application, copy the included example configuration file to your .config directory:"
echo "mkdir -p ~/.config/processtracker/"
echo "cp /usr/share/doc/processtracker/appsettings.json ~/.config/processtracker/"

%preun
if [ $1 -eq 0 ]; then  # Only on uninstallation, not upgrade
    echo "If you configured the application to run as service, run the following to stop and disable it:"
    echo "systemctl --user stop ProcessTracker.service"
    echo "systemctl --user disable ProcessTracker.service"
    echo "rm ~/.config/systemd/user/ProcessTracker.service"
    echo "systemctl --user daemon-reload"
fi

%postun
if [ $1 -eq 0 ] ; then
    echo "processtracker removed"
fi